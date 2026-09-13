#region License

// Copyright (c) 2005-2014, CellAO Team
// Copyright (c) 2026, OmniCell contributors
//
// This file is part of OmniCell and is distributed under the terms the project
// is licensed under. See the repository root for details.

#endregion

namespace LoginEngine.QueryBase
{
    #region Usings

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Xml.Linq;

    #endregion

    /// <summary>
    /// The name of the playfield a character is standing in, for the character
    /// selection screen.
    /// </summary>
    /// <remarks>
    /// The character list used to send the literal string "area unknown" for
    /// every character, which is what the selection screen showed. The names
    /// were never missing - they are in XML Data\Playfields.xml, which all four
    /// engines share a directory with - the login server just never looked.
    ///
    /// It reads the file directly rather than using ZoneEngine's Playfields
    /// class, because that lives in the zone server and this does not, and one
    /// name lookup is not worth a dependency between two processes that
    /// otherwise share nothing.
    ///
    /// Read once, on first use. The file does not change while the server runs.
    /// </remarks>
    public static class PlayfieldNames
    {
        #region Static Fields

        private static readonly Lazy<Dictionary<int, string>> Names =
            new Lazy<Dictionary<int, string>>(Load);

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The name of a playfield, or "area unknown" if it has none.
        /// </summary>
        /// <remarks>
        /// The fallback is the string this used to send unconditionally. A
        /// playfield the XML does not list is genuinely unknown here, and saying
        /// so is better than showing the number.
        /// </remarks>
        public static string Of(int playfieldId)
        {
            string name;
            return Names.Value.TryGetValue(playfieldId, out name) ? name : "area unknown";
        }

        #endregion

        #region Methods

        private static Dictionary<int, string> Load()
        {
            var names = new Dictionary<int, string>();

            try
            {
                string path = Path.Combine("XML Data", "Playfields.xml");
                if (!File.Exists(path))
                {
                    return names;
                }

                foreach (XElement playfield in XDocument.Load(path).Descendants("Playfield"))
                {
                    XAttribute id = playfield.Attribute("id");
                    XElement name = playfield.Element("Name");

                    int number;
                    if (id == null || name == null || !int.TryParse(id.Value, out number))
                    {
                        continue;
                    }

                    names[number] = name.Value;
                }
            }
            catch (Exception)
            {
                // A character list with no area names is worth having. A login
                // server that will not start because of them is not.
            }

            return names;
        }

        #endregion
    }
}
