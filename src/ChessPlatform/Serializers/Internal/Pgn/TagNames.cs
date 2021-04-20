using System;
using System.Linq;

namespace ChessPlatform.Serializers.Internal.Pgn
{
    public static class TagNames
    {
        public const string Event = "Event";
        public const string Site = "Site";
        public const string Date = "Date";
        public const string Round = "Round";
        public const string White = "White";
        public const string Black = "Black";
        public const string Result = "Result";

        public const string SetUp = "SetUp";
        public const string Fen = "FEN";
        public const string Variant = "Variant";

        public const string WhiteTitle = "WhiteTitle";
        public const string BlackTitle = "BlackTitle";

        public const string WhiteElo = "WhiteElo";
        public const string BlackElo = "BlackElo";

        public const string WhiteUscf = "WhiteUSCF";
        public const string BlackUscf = "BlackUSCF";

        //// ReSharper disable once InconsistentNaming
        public const string WhiteNA = "WhiteNA";

        //// ReSharper disable once InconsistentNaming
        public const string BlackNA = "BlackNA";

        public const string WhiteType = "WhiteType";
        public const string BlackType = "BlackType";

        public const string EventDate = "EventDate";
        public const string EventSponsor = "EventSponsor";
        public const string Section = "Section";
        public const string Stage = "Stage";
        public const string Board = "Board";

        public const string Opening = "Opening";
        public const string Variation = "Variation";
        public const string Subvariation = "SubVariation";

        public const string Eco = "ECO";
        public const string Nic = "NIC";

        public const string Time = "Time";
        public const string UtcTime = "UTCTime";
        public const string UtcDate = "UTCDate";

        public const string TimeControl = "TimeControl";
        public const string Termination = "Termination";
        public const string Annotator = "Annotator";
        public const string Mode = "Mode";
        public const string PlyCount = "PlyCount";
    }
}