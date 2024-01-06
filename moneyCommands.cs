using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TheBoysBot.commands
{
    public class moneyCommands : BaseCommandModule
    {
        private Dictionary<ulong, int> userWallets = new Dictionary<ulong, int>();
        private string connectionString = "Server=localhost;Database=bot;Uid=root;Pwd=;";

        private List<LeaderboardEntry> GetLeaderboardData()
        {
            var leaderboardData = new List<LeaderboardEntry>();

            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new MySqlCommand("SELECT UserId, WalletBalance, BankBalance FROM user_balances", connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userId = reader.GetUInt64("UserId");
                        var walletBalance = reader.GetInt32("WalletBalance");
                        var bankBalance = reader.GetInt32("BankBalance");

                        var totalMoney = walletBalance + bankBalance;

                        leaderboardData.Add(new LeaderboardEntry
                        {
                            UserId = userId,
                            TotalMoney = totalMoney
                        });
                    }
                }
            }
            leaderboardData = leaderboardData.OrderByDescending(entry => entry.TotalMoney).ToList();

            return leaderboardData;
        }

        [Command("balance")]
        [Description("Check your current wallet and bank balances.")]
        public async Task CheckBalance(CommandContext ctx)
        {
            try
            {
                var userBalance = GetUserBalance(ctx.User.Id);

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.User.Username}'s Balances",
                    Color = DiscordColor.Green,
                    Description = $"Wallet: {userBalance.WalletBalance} coins\nBank: {userBalance.BankBalance} coins"
                };

                await ctx.RespondAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking balance: {ex.Message}");
                await ctx.RespondAsync("An error occurred while fetching your balance. Please try again later.");
            }
        }

        [Command("deposit")]
        [Description("Deposit coins into your bank.")]
        [Aliases("dep")]
        public async Task Deposit(CommandContext ctx, int amount)
        {
            var userBalance = GetUserBalance(ctx.User.Id);

            if (amount <= 0 || amount > userBalance.WalletBalance)
            {
                await ctx.RespondAsync("Please deposit a valid amount.");
                return;
            }

            userBalance.WalletBalance -= amount;
            userBalance.BankBalance += amount;

            UpdateUserBalance(userBalance);

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{ctx.User.Username}'s Transaction",
                Color = DiscordColor.Green,
                Description = $"Deposited {amount} coins into your bank. Your new wallet balance is {userBalance.WalletBalance} coins."
            };

            await ctx.RespondAsync(embed: embed);
        }

        [Command("withdraw")]
        [Description("Withdraw coins from your bank to your wallet.")]
        [Aliases("with")]
        public async Task Withdraw(CommandContext ctx, int amount)
        {
            var userBalance = GetUserBalance(ctx.User.Id);

            if (amount <= 0 || amount > userBalance.BankBalance)
            {
                await ctx.RespondAsync("Please withdraw a valid amount.");
                return;
            }

            userBalance.WalletBalance += amount;
            userBalance.BankBalance -= amount;

            UpdateUserBalance(userBalance);

            var embed = new DiscordEmbedBuilder
            {
                Title = $"{ctx.User.Username}'s Transaction",
                Color = DiscordColor.Green,
                Description = $"Withdrew {amount} coins from your bank. Your new wallet balance is {userBalance.WalletBalance} coins."
            };

            await ctx.RespondAsync(embed: embed);
        }

        [Command("rob")]
        [Description("Attempt to rob someone's wallet.")]
        public async Task Rob(CommandContext ctx, DiscordMember targetMember)
        {
            try
            {
                var userBalance = GetUserBalance(ctx.User.Id);
                var targetBalance = GetUserBalance(targetMember.Id);

                int robAmount = 0;
                bool success = false;

                int robberyOutcome = new Random().Next(1, 101);

                if (robberyOutcome <= 70)
                {
                    robAmount = new Random().Next(1, 21);

                    if (robAmount > targetBalance.WalletBalance)
                        robAmount = targetBalance.WalletBalance;

                    success = true;
                }

                if (success)
                {
                    userBalance.WalletBalance += robAmount;
                    targetBalance.WalletBalance -= robAmount;

                    UpdateUserBalance(userBalance);
                    UpdateUserBalance(targetBalance);

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Robbery Successful",
                        Color = DiscordColor.Green,
                        Description = $"You successfully robbed {robAmount} coins from {targetMember.DisplayName}'s wallet! Your new wallet balance is {userBalance.WalletBalance} coins."
                    };

                    await ctx.RespondAsync(embed: embed);
                }
                else
                {
                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Robbery Failed",
                        Color = DiscordColor.Red,
                        Description = "Your attempt to rob failed. "
                    };


                    if (robberyOutcome <= 90)
                    {
                        int fine = 100;
                        userBalance.WalletBalance -= fine;

                        UpdateUserBalance(userBalance);

                        embed.Description += $"You were caught by the police and fined {fine} coins. Your new wallet balance is {userBalance.WalletBalance} coins.";
                    }
                    else
                    {
                        embed.Description += "Better luck next time!";
                    }

                    await ctx.RespondAsync(embed: embed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during robbery: {ex.Message}");
                await ctx.RespondAsync("An error occurred while attempting to rob. Please try again later.");
            }
        }

        [Command("dice")]
        [Description("Roll a six-sided die and win or lose money.")]
        public async Task RollDice(CommandContext ctx)
        {
            try
            {
                var userBalance = GetUserBalance(ctx.User.Id);

                int diceResult = new Random().Next(1, 7);

                int winAmount = 20;
                int loseAmount = 10;

                if (diceResult <= 3)
                {
                    userBalance.WalletBalance += winAmount;
                    UpdateUserBalance(userBalance);

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Dice Roll",
                        Color = DiscordColor.Green,
                        Description = $"You rolled a {diceResult}! You won {winAmount} coins. Your new balance is {userBalance.WalletBalance} coins."
                    };

                    await ctx.RespondAsync(embed: embed);
                }
                else
                {
                    userBalance.WalletBalance -= loseAmount;
                    UpdateUserBalance(userBalance);

                    var embed = new DiscordEmbedBuilder
                    {
                        Title = "Dice Roll",
                        Color = DiscordColor.Red,
                        Description = $"You rolled a {diceResult}. You lost {loseAmount} coins. Your new balance is {userBalance.WalletBalance} coins."
                    };

                    await ctx.RespondAsync(embed: embed);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during dice roll: {ex.Message}");
                await ctx.RespondAsync("An error occurred while playing the dice game. Please try again later.");
            }
        }

        [Command("leaderboard")]
        [Description("Check the money leaderboard.")]
        public async Task Leaderboard(CommandContext ctx)
        {
            try
            {
                var leaderboardData = GetLeaderboardData();

                var embed = new DiscordEmbedBuilder
                {
                    Title = "Money Leaderboard",
                    Color = DiscordColor.Gold,
                };

                int rank = 1;

                foreach (var entry in leaderboardData)
                {
                    var user = await ctx.Guild.GetMemberAsync(entry.UserId);

                    if (user != null)
                    {
                        embed.AddField($"{rank}. {user.DisplayName}", $"Total Money: {entry.TotalMoney} coins");
                        rank++;
                    }
                }

                await ctx.RespondAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching money leaderboard: {ex.Message}");
                await ctx.RespondAsync("An error occurred while fetching the money leaderboard. Please try again later.");
            }
        }

        private UserBalance GetUserBalance(ulong userId)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new MySqlCommand($"SELECT * FROM user_balances WHERE UserId = {userId}", connection))
                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new UserBalance
                        {
                            UserId = Convert.ToUInt64(reader["UserId"]),
                            WalletBalance = Convert.ToInt32(reader["WalletBalance"]),
                            BankBalance = Convert.ToInt32(reader["BankBalance"])
                        };
                    }
                    else
                    {
                        var newUserBalance = new UserBalance { UserId = userId, WalletBalance = 100, BankBalance = 0 };
                        InsertUserBalance(newUserBalance);
                        return newUserBalance;
                    }
                }
            }
        }

        private void UpdateUserBalance(UserBalance userBalance)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    using (var command = new MySqlCommand($"UPDATE user_balances SET WalletBalance = {userBalance.WalletBalance}, BankBalance = {userBalance.BankBalance} WHERE UserId = {userBalance.UserId}", connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating user balance: {ex.Message}");
                    throw;
                }
            }
        }

        private void InsertUserBalance(UserBalance userBalance)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new MySqlCommand($"INSERT INTO user_balances (UserId, WalletBalance, BankBalance) VALUES ({userBalance.UserId}, {userBalance.WalletBalance}, {userBalance.BankBalance})", connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }
    }

    public class UserBalance
    {
        public ulong UserId { get; set; }
        public int WalletBalance { get; set; }
        public int BankBalance { get; set; }
    }

    public class LeaderboardEntry
    {
        public ulong UserId { get; set; }
        public int TotalMoney { get; set; }
    }
}

