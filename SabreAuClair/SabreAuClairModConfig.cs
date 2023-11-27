namespace SabreAuClair {
    public class SabreAuClairModConfig {
        /** <summary> Mercenary wage in rusty gear per ingame day </summary> */ public int MercenaryDailyWage         = 1;

        /** <summary> Maximum count of company member </summary> */   public int   CompanyCapacity       = 16;
        /** <summary> Row per column ratio `row/column` </summary> */ public float LineFormationRowRatio = 0.25f;

        /** <summary> Array of collectible codes that can be used to lead a company </summary> */ public string[] CommanderToolCodes         = System.Array.Empty<string>();
        /** <summary> Indicates to use any `ItemBlade` item as a commander tool </summary> */     public bool     UseAnyBladeAsCommanderTool = false;
        /** <summary> Indicates to ignore `*-ruined` variant commander tools </summary> */        public bool     IgnoreRuinedCommanderTools = true;
        /** <summary> Indicates to ignore `*-admin` variant commander tools </summary> */         public bool     IgnoreAdminCommanderTools  = true;
    } // class ..
} // namespace ..
