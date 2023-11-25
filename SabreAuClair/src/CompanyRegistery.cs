using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Client;
using Vintagestory.API.Config;


namespace SabreAuClair {
    public class CompanyRegistery : ModSystem {

        //=======================
        // D E F I N I T I O N S
        //=======================
            
            /** <summary> Reference to every player's company </summary> **/ private readonly Dictionary<IPlayer, Company> companiesByPlayer = new();


        //===============================
        // I N I T I A L I Z A T I O N S
        //===============================

            public override bool ShouldLoad(EnumAppSide forSide) => true;


        //===============================
        // I M P L E M E N T A T I O N S
        //===============================

            /// <summary>
            /// Gets the company associated with a given player if it exists
            /// </summary>
            /// <param name="player"></param>
            /// <param name="company"></param>
            /// <returns></returns>
            public bool TryGetCompany(IPlayer player, out Company company) =>
                this.companiesByPlayer.TryGetValue(player, out company);


            /// <summary>
            /// Sets a company command if the company exists
            /// </summary>
            /// <param name="byPlayer"></param>
            /// <param name="command"></param>
            /// <param name="state"></param>
            /// <param name="formationCommand"></param>
            public void SetCompanyCommand(
                IPlayer byPlayer,
                string  command,
                bool    state,
                bool    formationCommand = true
            ) {
                if (this.companiesByPlayer.TryGetValue(byPlayer, out Company company)) {
                    if (!company.Commands.TryAdd(command, state))
                        company.Commands[command] = state;

                    if (formationCommand) foreach (IHireable hireable in company.MembersInFormation)
                        hireable.Entity.WatchedAttributes.SetBool(command, state);

                    else foreach (IHireable hireable in company.Members)
                        hireable.Entity.WatchedAttributes.SetBool(command, state);

                } // if ..
            } // void ..


            /// <summary>
            /// Switch a company command state if it exists
            /// </summary>
            /// <param name="byPlayer"></param>
            /// <param name="command"></param>
            /// <param name="formationCommand"></param>
            public void SwitchCompanyCommand(
                IPlayer byPlayer,
                string  command,
                bool    formationCommand = true
            ) {
                if (this.companiesByPlayer.TryGetValue(byPlayer, out Company company)) {
                    if (!company.Commands.TryAdd(command, true))
                        company.Commands[command] = !company.Commands[command];

                    bool state = company.Commands[command];

                    if (formationCommand) foreach (IHireable hireable in company.MembersInFormation)
                        hireable.Entity.WatchedAttributes.SetBool(command, state);

                    else foreach (IHireable hireable in company.Members)
                        hireable.Entity.WatchedAttributes.SetBool(command, state);

                } // if ..
            } // void ..


            /// <summary>
            /// Tries to add a hireable entity to a player's company
            /// </summary>
            /// <param name="byPlayer"></param>
            /// <param name="hireable"></param>
            /// <returns></returns>
            public bool TryAddMember(
                IPlayer byPlayer,
                IHireable hireable
            ) {
                if (this.companiesByPlayer.TryGetValue(byPlayer, out Company company)) {
                    if (!company.Members.Contains(hireable))
                        if(company.Members.Count < SabreAuClairModSystem.GlobalConstants.CompanyCapacity) {
                            
                            company.Members.Add(hireable);
                            if (hireable.Entity.WatchedAttributes.GetBool("commandFormation"))
                                if (!company.MembersInFormation.Contains(hireable))
                                    company.MembersInFormation.Add(hireable);

                        } else {
                            if (byPlayer.Entity.Api is ICoreClientAPI clientApi)
                                clientApi.TriggerIngameError(this, "companylimitreached", Lang.Get("ingameerror-companylimitreached"));
                                
                            return false;

                        } // if ..

            } else this.companiesByPlayer.Add(byPlayer, new() {
                    Members            = new(SabreAuClairModSystem.GlobalConstants.CompanyCapacity) { hireable },
                    MembersInFormation = new(SabreAuClairModSystem.GlobalConstants.CompanyCapacity),
                    Formation          = EnumFormation.Line,
                    Commands           = new()
                }); // ..

                return true;
                
            } // bool ..


            /// <summary>
            /// Registers a hireable inside its matching company if it exists
            /// </summary>
            /// <param name="hireable"></param>
            public void RegisterCompanyMember(IHireable hireable) {
                if (hireable.Commander == null) return;
                else if (this.companiesByPlayer.TryGetValue(hireable.Commander, out Company company)) {
                    company.Members.Add(hireable);
                    if (hireable.Entity.WatchedAttributes.GetBool("commandFormation"))
                        if (!company.MembersInFormation.Contains(hireable))
                            company.MembersInFormation.Add(hireable);

                } else this.companiesByPlayer.Add(hireable.Commander, new() {
                    Members            = new(SabreAuClairModSystem.GlobalConstants.CompanyCapacity) { hireable },
                    MembersInFormation = new(SabreAuClairModSystem.GlobalConstants.CompanyCapacity),
                    Formation          = EnumFormation.Line,
                    Commands           = new()
                }); // if ..
            } // void ..
            

            /// <summary>
            /// Adds a company member to its formation
            /// </summary>
            /// <param name="hireable"></param>
            public void AddCompanyMemberToFormation(IHireable hireable) {
                if (hireable.Commander != null && this.companiesByPlayer.TryGetValue(hireable.Commander, out Company company)) {

                    hireable.Entity.WatchedAttributes.SetBool("commandFormation", true);
                    if (!company.MembersInFormation.Contains(hireable))
                        company.MembersInFormation.Add(hireable);
                    
                } // if ..
            } // void ..


            /// <summary>
            /// Removes a company member if the company exists
            /// </summary>
            /// <param name="hireable"></param>
            public void RemoveCompanyMember(IHireable hireable) {
                if (hireable.Commander != null && this.companiesByPlayer.TryGetValue(hireable.Commander, out Company company)) {
                    
                    company.Members.Remove(hireable);
                    company.MembersInFormation.Remove(hireable);

                } // if ..
            } // void ..


            /// <summary>
            /// Removes a company member from the company's formation if it exists
            /// </summary>
            /// <param name="hireable"></param>
            public void RemoveCompanyMemberFromFormation(IHireable hireable) {
                if (hireable.Commander != null && this.companiesByPlayer.TryGetValue(hireable.Commander, out Company company)) {

                    company.MembersInFormation.Remove(hireable);
                    hireable.Entity.WatchedAttributes.SetBool("commandFormation", false);
                    
                } // if ..
            } // void ..


            /// <summary>
            /// Gets a hireable target position based on the company's formation
            /// </summary>
            /// <param name="hireable"></param>
            /// <param name="targetPos"></param>
            /// <returns></returns>
            public bool TryGetTargetPos(
                IHireable hireable,
                out Vec3d targetPos
            ) {
                if (hireable.Commander == null) {

                    targetPos = null;
                    return false;

                } else if (this.companiesByPlayer.TryGetValue(hireable.Commander, out Company company)) {

                    int columnCount = (int)GameMath.Sqrt((float)company.Members.Count / SabreAuClairModSystem.GlobalConstants.LineFormationRowRatio);
                    if (company.MembersInFormation.IndexOf(hireable) is int index && index != -1) {

                        switch (company.Formation) {
                            case EnumFormation.Line: {

                                EntityPos basePos = hireable.Commander.Entity.ServerPos;

                                int column = index % columnCount;
                                int row    = index / columnCount + 1;

                                float cos = GameMath.Cos(basePos.Yaw);
                                float sin = GameMath.Sin(basePos.Yaw);

                                targetPos = basePos.XYZ
                                    + new Vec3d( sin, 0, cos) * (column + 1 - GameMath.Min(company.Members.Count, columnCount) * 0.5)
                                    + new Vec3d(-cos, 0, sin) * row * 2;

                                return true;

                            } // case ..
                            default: {

                                targetPos = null;
                                return false;
                                
                            } // default ..
                        } // switch ..
                    } else {
                
                        targetPos = null;
                        return false;

                    } // if ..
                } else {

                    targetPos = null;
                    return false;

                } // if ..
            } // Vec3d ..
    } // class ..
} // namespace 
