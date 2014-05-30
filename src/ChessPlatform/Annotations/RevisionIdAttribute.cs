using System;
using System.Linq;
using Omnifactotum.Annotations;

namespace ChessPlatform.Annotations
{
    public sealed class RevisionIdAttribute : Attribute
    {
        #region Constructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="RevisionIdAttribute"/> class
        ///     using the specified revision identifier.
        /// </summary>
        public RevisionIdAttribute([NotNull] string revisionId)
        {
            #region Argument Check

            if (string.IsNullOrWhiteSpace(revisionId))
            {
                throw new ArgumentException(
                    @"The value can be neither empty nor whitespace-only string nor null.",
                    "revisionId");
            }

            #endregion

            this.RevisionId = revisionId;
        }

        #endregion

        #region Public Properties

        public string RevisionId
        {
            get;
            private set;
        }

        #endregion
    }
}