using System;
using System.Linq;

namespace ChessPlatform
{
    public static class EnPassantCaptureInfoExtensions
    {
        #region Public Methods

        public static string GetFenSnippet(this EnPassantCaptureInfo enPassantCaptureInfo)
        {
            return enPassantCaptureInfo?.CapturePosition.ToString() ?? ChessConstants.NoEnPassantCaptureFenSnippet;
        }

        #endregion
    }
}