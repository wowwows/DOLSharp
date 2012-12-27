﻿/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

using System;
using System.Text.RegularExpressions;
using DOL.GS.Commands;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.Privilege
{
    [CmdAttribute(
        Cmd = "&privilege",
        Aliases = new[]
            {
                "&modpriv"
            },
        Level = (uint) ePrivLevel.Admin, 
        Privilege = "privilege_mod",
        Description = "Privilege.Description", 
        Usage = new string[]
            {
             
            })]
    public class PrivilegeCommand : AbstractCommandHandler, ICommandHandler
    {
        private static readonly Regex CompactSyntaxRagex = new Regex(@"(\+|-)(a)?/(cmd|c|grp|g|priv|p)/([a-zA-Z_0-9]+)");

        public void OnCommand(GameClient client, string[] args)
        {
            if (!Properties.USE_NEW_PRIVILEGE_SYSTEM)
            {
                DisplayMessage(client, "This command is not available whilst using the legacy system.");
                return;
            }

            if (args.Length == 2 || args.Length == 3)
            {
                if (CompactSyntaxRagex.IsMatch(args[1]))
                {
                    HandleCompactSyntax(client, CompactSyntaxRagex.Match(args[1]), args.Length == 3 ? args[2] : null);
                }
            }

            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            GamePlayer target = client.Player;

            // Use a switch with space-separated arguments.
            switch (args[1])
            {
            #region Add to Target
                case "+":
                case "add":
                    break;
                
                case "++":
                case "addacct":
                case "addaccount":
                    break;
            #endregion

            #region Delete from Target
                case "del":
                case "remove":
                    break;

                case "delacct":
                case "removeaccount":
                    break;
            #endregion
            }
        }


        /// Compact Syntax is as Follows.
        /// 
        /// To add something to a player
        /// /privilege +/<TYPE>/<VALUE> <OPTIONAL TARGET>
        /// 
        /// To remove something from a player
        /// /privilege -/<TYPE>/<VALUE> <OPTIONAL TARGET>
        /// 
        /// To add something to an account
        /// /privilege +a/<TYPE>/<VALUE> <OPTIONAL TARGET>
        /// 
        /// To remove something from an account
        /// /privilege -a/<TYPE>/<VALUE> <OPTIONAL TARGET>
        /// 
        /// Where <TYPE> is
        ///      For Commands    c, cmd
        ///      For Groups      g, grp
        ///      For Privileges  p, priv
        /// 
        /// Where VALUE is dependent upon the TYPE.


        /// <summary>
        /// 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="compactParts"></param>
        /// <param name="additionalArguments"></param>
        private void HandleCompactSyntax(GameClient client, Match compactParts, string optionalTarget)
        {
            bool performAdd = compactParts.Groups[1].Value == "+";
            bool targetAccount = compactParts.Groups[2].Success;
            ModificationType type = ModificationType.None;
            switch (compactParts.Groups[3].Value)
            {
                case "c":
                case "cmd":
                    type = ModificationType.Command;
                    break;
                
                case "g":
                case "grp":
                    type = ModificationType.Group;
                    break;

                case "p":
                case "priv":
                    type = ModificationType.Privilege;
                    break;
            }
            string value = compactParts.Groups[4].Value;

            GameClient target = client;

            if (!string.IsNullOrEmpty(optionalTarget))
            {
                if (optionalTarget == "!" && client.Player.TargetObject is GamePlayer)
                    target = (client.Player.TargetObject as GamePlayer).Client;
            }

            PrivilegeBinding targetBinding = targetAccount ? 
                target.AccountPrivileges : target.Player.PlayerPrivileges;


            switch (type)
            {
                #region Command
                case ModificationType.Command:
                    {
                        ModificationStatus result;

                        if (performAdd)
                        {
                            result = targetBinding.AddCommand(value);
                            if (result == ModificationStatus.Success)
                            {
                                if (client != target)
                                {
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.GrantPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value, target.Player.Name));

                                    if (client.Player.IsAnonymous)
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.GainPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value));
                                    else
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.ReceivedPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value, client.Player.Name));
                                }
                                else
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.GainPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value));
                            }
                        }
                        else
                        {
                            result = targetBinding.RemoveCommand(value);
                            if (result == ModificationStatus.Success)
                            {
                                if (client != target)
                                {
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.RevokePrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value, target.Player.Name));

                                    if (client.Player.IsAnonymous)
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.LostPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value));
                                    else
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.RevokedPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value, client.Player.Name));
                                }
                                else
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.LostPrefix + (targetAccount ? "CommandAccount" : "CommandPlayer"), value));
                            }
                        }
                    }
                    break;
                #endregion

                #region Privilege
                case ModificationType.Privilege:
                    {
                        ModificationStatus result;

                        if (performAdd)
                        {
                            result = targetBinding.AddPrivilege(value);
                            if (result == ModificationStatus.Success)
                            {
                                if (client != target)
                                {
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.GrantPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value, target.Player.Name));

                                    if (client.Player.IsAnonymous)
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.GainPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value));
                                    else
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.ReceivedPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value, client.Player.Name));
                                }
                                else
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.GainPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value));
                            }
                            else if (result == ModificationStatus.AlreadyExists)
                                DisplayMessage(client, LanguageMgr.GetTranslation
                                    (target.Account.Language, PrivilegeDefaults.ErrorPrefix + "AlreadyHas", value, target.Player.Name));
                        }
                        else
                        {
                            result = targetBinding.RemovePrivilege(value);
                            if (result == ModificationStatus.Success)
                            {
                                if (client != target)
                                {
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.RevokePrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value, target.Player.Name));

                                    if (client.Player.IsAnonymous)
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.LostPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value));
                                    else
                                        DisplayMessage(target, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.RevokedPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value, client.Player.Name));
                                }
                                else
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                        (target.Account.Language, PrivilegeDefaults.LostPrefix + (targetAccount ? "PrivilegeAccount" : "PrivilegePlayer"), value));
                            }
                            else if(result == ModificationStatus.DoesNotExist)
                                DisplayMessage(client, LanguageMgr.GetTranslation
                                    (target.Account.Language, PrivilegeDefaults.ErrorPrefix + "DoesNotHave", value, target.Player.Name));
                        }
                    }
                    break;
                #endregion

                #region Group
                case ModificationType.Group:
                    {
                        PrivilegeGroup targetGroup = PrivilegeManager.GetGroup(value);

                        if (targetGroup != null)
                        {
                            ModificationStatus result;
                            if (performAdd)
                            {
                                result = targetBinding.AddGroup(targetGroup);
                                if (result == ModificationStatus.Success)
                                {
                                    if (client != target)
                                    {
                                        DisplayMessage(client, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.GrantPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name, target.Player.Name));

                                        if (client.Player.IsAnonymous)
                                            DisplayMessage(target, LanguageMgr.GetTranslation
                                                (target.Account.Language, PrivilegeDefaults.GainPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name));
                                        else
                                            DisplayMessage(target, LanguageMgr.GetTranslation
                                                (target.Account.Language, PrivilegeDefaults.ReceivedPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name, client.Player.Name));
                                    }
                                    else
                                        DisplayMessage(client, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.GainPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name));
                                }
                            }
                            else
                            {
                                result = targetBinding.RemoveGroup(targetGroup);
                                if (result == ModificationStatus.Success)
                                {
                                    if (client != target)
                                    {
                                        DisplayMessage(client, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.RevokePrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name, target.Player.Name));

                                        if (client.Player.IsAnonymous)
                                            DisplayMessage(target, LanguageMgr.GetTranslation
                                                (target.Account.Language, PrivilegeDefaults.LostPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name));
                                        else
                                            DisplayMessage(target, LanguageMgr.GetTranslation
                                                (target.Account.Language, PrivilegeDefaults.RevokedPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name, client.Player.Name));
                                    }
                                    else
                                        DisplayMessage(client, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.LostPrefix + (targetAccount ? "GroupAccount" : "GroupPlayer"), targetGroup.DBEntry.Name));
                                }
                                else if(result == ModificationStatus.DoesNotExist)
                                    DisplayMessage(client, LanguageMgr.GetTranslation
                                            (target.Account.Language, PrivilegeDefaults.GroupErrorPrefix + "DoesNotHave", targetGroup.DBEntry.Name, target.Player.Name));
                            }
                        }
                        else
                            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, PrivilegeDefaults.GroupErrorPrefix + "NotFound", value));
                    }
                    break;
                #endregion

                case ModificationType.None:
                       // TODO: Notify them that their type wasn't valid.
                    break;
            }
        }

        private enum ModificationType
        {
            Command,
            Privilege,
            Group,
            None
        }
    }
}
