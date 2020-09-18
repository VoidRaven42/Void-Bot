using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using HSNXT.DSharpPlus.ModernEmbedBuilder;
using MoreLinq;

namespace Void_Bot
{
    public class HelpFormatter : BaseHelpFormatter
    {
        public HelpFormatter(CommandContext ctx) : base(ctx)
        {
            Ctx = ctx;
            Guild = ctx.Guild;
            EmbedBuilder = new ModernEmbedBuilder
            {
                Title = "Command list",
                Color = DiscordColor.Purple
            }.AddGeneratedForFooter(ctx);
        }

        private CommandContext Ctx { get; }
        private Command Command { get; set; }
        private DiscordGuild Guild { get; }
        public ModernEmbedBuilder EmbedBuilder { get; }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            Command = command;

            EmbedBuilder.Description =
                $"{Formatter.InlineCode(command.Name)}: {command.Description ?? "No description provided."}";

            if (command is CommandGroup cgroup && cgroup.IsExecutableWithoutSubcommands)
                EmbedBuilder.Description =
                    $"{EmbedBuilder.Description}\n\nThis group can be executed as a standalone command.";

            if (command.Aliases?.Any() == true)
                EmbedBuilder.AddField("Aliases", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)));

            if (command.Overloads?.Any() == true)
            {
                var sb = new StringBuilder();

                foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
                {
                    sb.Append('`').Append(command.QualifiedName);

                    foreach (var arg in ovl.Arguments)
                        sb.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name)
                            .Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');

                    sb.Append("`\n");

                    foreach (var arg in ovl.Arguments)
                        sb.Append('`').Append(arg.Name).Append(" (")
                            .Append(Program.Commands[0].GetUserFriendlyTypeName(arg.Type)).Append(")`: ")
                            .Append(arg.Description).Append('\n');

                    sb.Append('\n');
                }

                EmbedBuilder.AddField("Arguments", sb.ToString().Trim());
            }

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            if (Command == null) EmbedBuilder.AddField("Prefix", Ctx.Prefix, true);
            var categories = subcommands.Where(x => x.Name != "help" && x.IsHidden == false).Select(c => c.Category())
                .DistinctBy(x => x);
            foreach (var category in categories)
                EmbedBuilder.AddField(Command != null ? "Subcommands" : category,
                    string.Join(", ", subcommands.Where(c => c.Category() == category).Select(x => $"`{x.Name}`")));
            if (Command == null)
                EmbedBuilder.AddField("For more information type", $"{Ctx.Prefix}help <command>", true);
            //EmbedBuilder.AddField("Support server", "[Join our support server](https://discord.gg/n3ZYNtV)", true);
            else
                EmbedBuilder.AddField("For more information type", $"{Ctx.Prefix}help {Command.Name} <subcommand>");

            return this;
        }

        public override CommandHelpMessage Build()
        {
            return new CommandHelpMessage(embed: EmbedBuilder.Build());
        }
    }

    public static class EmbedUtils
    {
        public static DuckColor Red = new DuckColor(231, 76, 60);

        public static async Task<string> Lang(this CommandContext ctx, string term)
        {
            return "Void Bot";
        }

        public static ModernEmbedBuilder AddGeneratedForFooter(this ModernEmbedBuilder embed, CommandContext ctx,
            bool defaultColor = true)
        {
            embed.Timestamp = DuckTimestamp.Now;
            embed.FooterText = ctx.Lang("global.footer").GetAwaiter().GetResult().Replace("{user}", ctx.User.Username);
            embed.FooterIcon = ctx.User.AvatarUrl;
            return embed;
        }

        public static string Category(this Command command)
        {
            var categoryAttribute =
                (CategoryAttribute) command.CustomAttributes.FirstOrDefault(x => x is CategoryAttribute);
            return categoryAttribute != null
                ? categoryAttribute.Category
                : command.Module.ModuleType.Namespace?.Split('.').Last();
        }
    }
}