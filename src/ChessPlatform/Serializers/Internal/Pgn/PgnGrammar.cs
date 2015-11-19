using System;
using System.Linq;
using Irony.Parsing;

namespace ChessPlatform.Serializers.Internal.Pgn
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

        public PgnGrammar()
        {
            var pgnDatabase = new NonTerminal("PGN-database", typeof(DatabaseAstNode));
            var pgnGame = new NonTerminal("PGN-game", typeof(GameAstNode));
            var tagSection = new NonTerminal("tag-section", typeof(TagSectionAstNode));
            var tagPair = new NonTerminal("tag-pair", typeof(TagPairAstNode));
            var tagName = new IdentifierTerminal("tag-name") { AstConfig = { NodeType = typeof(TagNameAstNode) } };

            var tagValue = new RegexBasedTerminal("tag-value", @"[^""\r\n]*")
            {
                AstConfig = { NodeType = typeof(TagValueAstNode) }
            };

            var movetextSection = new NonTerminal("movetext-section", typeof(MovetextSectionAstNode));
            var elementSequence = new NonTerminal("element-sequence", typeof(ElementSequenceAstNode));
            var element = new NonTerminal("element", typeof(ElementAstNode));
            var recursiveVariation = new NonTerminal("recursive-variation") { Flags = TermFlags.NoAstNode };
            var gameTermination = new NonTerminal("game-termination", typeof(GameTerminationAstNode));

            var moveNumberIndication = new RegexBasedTerminal("move-number", @"\d+\.(\.\.)?")
            {
                AstConfig = { NodeType = typeof(MoveNumberIndicationAstNode) }
            };

            var sanMove = new RegexBasedTerminal(
                "SAN-move",
                $@"(({PiecePattern}?{FilePattern}?{RankPattern}?[x]?({FilePattern}{RankPattern})(\={PromotionPattern
                    })?)|(O-O(-O)?))[+#]?")
            {
                AstConfig = { NodeType = typeof(SanMoveAstNode) }
            };

            var numericAnnotationGlyph = new RegexBasedTerminal(
                "numeric-annotation-glyph",
                @"(\$\d+)|[\?\!]{1,2}")
            {
                Flags = TermFlags.NoAstNode
            };

            var singleLineComment = new CommentTerminal("single-line-comment", ";", "\n", "\r")
            {
                AstConfig = { NodeType = typeof(CommentAstNode) }
            };

            var multiLineComment = new CommentTerminal("multi-line-comment", "{", "}")
            {
                AstConfig = { NodeType = typeof(CommentAstNode) }
            };

            pgnDatabase.Rule = MakeStarRule(pgnDatabase, pgnGame);
            pgnGame.Rule = tagSection + movetextSection;
            tagSection.Rule = MakeStarRule(tagSection, tagPair);
            tagPair.Rule = "[" + tagName + "\"" + tagValue + "\"" + "]";
            movetextSection.Rule = elementSequence + gameTermination;
            elementSequence.Rule = MakeStarRule(elementSequence, element);
            element.Rule = moveNumberIndication | sanMove | numericAnnotationGlyph | recursiveVariation;
            recursiveVariation.Rule = "(" + elementSequence + ")";
            gameTermination.Rule = (BnfExpression)"1-0" | "0-1" | "1/2-1/2" | "*";

            NonGrammarTerminals.Add(singleLineComment);
            NonGrammarTerminals.Add(multiLineComment);

            MarkPunctuation("[", "]", "\"", ".", "...", "(", ")");

            Root = pgnDatabase;
            LanguageFlags = LanguageFlags.CreateAst;
        }

        #endregion
    }
}