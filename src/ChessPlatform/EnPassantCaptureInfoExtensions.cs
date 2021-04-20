using System;
using System.Linq;

namespace ChessPlatform
{
    public static class EnPassantCaptureInfoExtensions
    {
        public static string GetFenSnippet(this EnPassantCaptureInfo enPassantCaptureInfo)
        {
            return enPassantCaptureInfo?.CaptureSquare.ToString() ?? ChessConstants.NoEnPassantCaptureFenSnippet;
        }
    }
}