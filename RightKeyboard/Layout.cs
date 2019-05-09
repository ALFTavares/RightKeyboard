using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Globalization;

namespace RightKeyboard
{
    /// <summary>
    /// Represents a keyboard layout
    /// </summary>
    public class Layout
    {
        #region properties

        /// <summary>
        /// Gets the layout's identifier
        /// </summary>
        public ushort Identifier { get; }

        /// <summary>
        /// Gets the layout's name
        /// </summary>
        public string Name { get; }

        private static Layout[] _cachedLayouts;

        #endregion

        #region CTOR

        /// <summary>
        /// Initializes a new instance of Layout
        /// </summary>
        /// <param name="identifier"></param>
        /// <param name="name"></param>
        public Layout(ushort identifier, string name)
        {
            Identifier = identifier;
            Name = name;
        }

        #endregion

        #region public methods

        /// <summary>
        /// Gets the keyboard layouts from a resource file
        /// </summary>
        /// <returns></returns>
        public static Layout[] GetLayouts()
        {
            var projectFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
            var file = Path.Combine(projectFolder, @"resources\Layouts.txt");

            if (_cachedLayouts != null)
                return _cachedLayouts;

            var layouts = new List<Layout>();
            using (var input = File.OpenRead(file))
            {
                using (TextReader reader = new StreamReader(input ?? throw new FileNotFoundException("Couldn't locate Layouts.txt file")))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        layouts.Add(GetLayout(line));
                    }
                }
            }
            _cachedLayouts = layouts.ToArray();
            return _cachedLayouts;
        }

        public override string ToString()
        {
            return Name;
        }

        #endregion

        #region private methods

        private static Layout GetLayout(string line)
        {
            var parts = line.Trim().Split('=');

            var identifier = ushort.Parse(parts[0], NumberStyles.HexNumber);
            var name = parts[1];
            return new Layout(identifier, name);
        }

        #endregion
    }
}