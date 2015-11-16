using System;
using System.Linq;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal
{
    internal sealed class PgnGrammar : Grammar
    {
        #region Constants and Fields

        private const string FilePattern = "[abcdefgh]";
        private const string RankPattern = "[12345678]";
        private const string PiecePattern = "[KQRBNP]"; // Though not recommended, P may be used for pawn
        private const string PromotionPattern = "[QRBN]";

        #endregion

        #region Constructors

        internal PgnGrammar()
        {
            var pgnDatabase = new NonTerminal("PGN-database");
            var pgnGame = new NonTerminal("PGN-game");
            var tagSection = new NonTerminal("tag-section");
            var tagPair = new NonTerminal("tag-pair");
            var tagName = new IdentifierTerminal("tag-name");
            var tagValue = new RegexBasedTerminal("tag-value", @"[^""\r\n]*");
            var movetextSection = new NonTerminal("movetext-section");
            var elementSequence = new NonTerminal("element-sequence");
            var element = new NonTerminal("element");
            var recursiveVariation = new NonTerminal("recursive-variation");
            var gameTermination = new NonTerminal("game-termination");
            var moveNumber = new NumberLiteral("move-number", NumberOptions.IntOnly);

            var moveNumberIndication = new NonTerminal("move-number-indication");

            var sanMove = new RegexBasedTerminal(
                "SAN-move",
                $@"(({PiecePattern}?{FilePattern}?{RankPattern}?[x]?({FilePattern}{RankPattern})(\={PromotionPattern
                    })?)|(O-O(-O)?))[+#]?");

            var numericAnnotationGlyph = new RegexBasedTerminal(
                "numeric-annotation-glyph",
                @"(\$\d+)|[\?\!]{1,2}");

            var singleLineComment = new CommentTerminal("single-line-comment", ";", "\n", "\r");
            var multiLineComment = new CommentTerminal("multi-line-comment", "{", "}");

            pgnDatabase.Rule = MakeStarRule(pgnDatabase, pgnGame);
            pgnGame.Rule = tagSection + movetextSection;
            tagSection.Rule = MakeStarRule(tagSection, tagPair);
            tagPair.Rule = "[" + tagName + "\"" + tagValue + "\"" + "]";
            movetextSection.Rule = elementSequence + gameTermination;
            ////elementSequence.Rule = (element + elementSequence) | (recursiveVariation + elementSequence) | Empty;
            elementSequence.Rule = MakeStarRule(elementSequence, element | recursiveVariation);
            element.Rule = moveNumberIndication | sanMove | numericAnnotationGlyph;
            moveNumberIndication.Rule = moveNumber + "." + ((BnfExpression)"..").Q();
            recursiveVariation.Rule = "(" + elementSequence + ")";
            gameTermination.Rule = (BnfExpression)"1-0" | "0-1" | "1/2-1/2" | "*";

            NonGrammarTerminals.Add(singleLineComment);
            NonGrammarTerminals.Add(multiLineComment);

            MarkPunctuation("[", "]", "\"", ".", "...");

            Root = pgnDatabase;
        }

        #endregion
    }
}