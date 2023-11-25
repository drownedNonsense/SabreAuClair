using System.Collections.Generic;


namespace SabreAuClair {
    public struct Company {
        /** <summary> Reference to each company member </summary> **/                      public HashSet<IHireable>       Members;
        /** <summary> Reference to each company member inside the formation </summary> **/ public List<IHireable>          MembersInFormation;
        /** <summary> Formation type </summary> **/                                        public EnumFormation            Formation;
        /** <summary> Reference to each active command </summary> **/                      public Dictionary<string, bool> Commands;
    } // class ..
} // namespace 
