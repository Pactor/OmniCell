#region License

// Copyright (c) 2005-2014, CellAO Team
// 
// 
// All rights reserved.
// 
// 
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
// 
// 
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//     * Neither the name of the CellAO Team nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
// 
// 
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
// CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
// EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
// PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
// LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
// NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
// SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

#endregion

namespace Utility
{
    #region Usings ...

    using System;

    #endregion

    /// <summary>
    /// </summary>
    public static class OnScreenBanner
    {
        #region Public Methods and Operators

        /// <summary>
        /// </summary>
        /// <param name="titleColor">
        /// </param>
        public static void PrintBanner(ConsoleColor titleColor)
        {
            int consoleWidth = SafeWidth();

            // When output is redirected there is no auto-wrap at a window edge, so the
            // explicit newlines must be written instead of relying on the console.
            bool sameWidth = OutputIsConsole && (consoleWidth == SafeBufferWidth());

            if (OutputIsConsole)
            {
                try
                {
                    Console.Clear();
                }
                catch (Exception)
                {
                    // Clearing is cosmetic; never let it stop a server from starting.
                }
            }

            Console.Write("**".PadRight(consoleWidth, '*'));
            if (!sameWidth)
            {
                Console.WriteLine();
            }

            CenteredString(string.Empty, "**");
            if (!sameWidth)
            {
                Console.WriteLine();
            }

            CenteredString("OmniCell " + AssemblyInfoclass.Title, "**", titleColor);
            if (!sameWidth)
            {
                Console.WriteLine();
            }

            CenteredString(AssemblyInfoclass.AssemblyVersion, "**", ConsoleColor.White);
            if (!sameWidth)
            {
                Console.WriteLine();
            }

            CenteredString(AssemblyInfoclass.RevisionName, "**", ConsoleColor.DarkGray);
            if (!sameWidth)
            {
                Console.WriteLine();
            }

            CenteredString(string.Empty, "**");
            if (!sameWidth)
            {
                Console.WriteLine();
            }

            Console.Write("**".PadRight(consoleWidth, '*'));
            if (!sameWidth)
            {
                Console.WriteLine();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Width to fall back to when the real console width cannot be read: Mono reports
        /// 0, and a redirected stdout has no width at all.
        /// </summary>
        private const int FallbackWidth = 80;

        /// <summary>
        /// True only when stdout is a real console.
        /// Console.WindowWidth, Console.BufferWidth and Console.Clear all query the stdout
        /// handle and throw IOException ("The handle is invalid") when it is a file or a
        /// pipe. That is every service, supervisor or piped-log launch, so reading console
        /// geometry unguarded makes the process die before Main does any work.
        /// </summary>
        private static bool OutputIsConsole
        {
            get
            {
                try
                {
                    return !Console.IsOutputRedirected;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Console window width, or <see cref="FallbackWidth"/> when it cannot be read.
        /// </summary>
        private static int SafeWidth()
        {
            if (!OutputIsConsole)
            {
                return FallbackWidth;
            }

            try
            {
                int width = Console.WindowWidth;
                return width > 0 ? width : FallbackWidth;
            }
            catch (Exception)
            {
                return FallbackWidth;
            }
        }

        /// <summary>
        /// Console buffer width, or <see cref="FallbackWidth"/> when it cannot be read.
        /// </summary>
        private static int SafeBufferWidth()
        {
            if (!OutputIsConsole)
            {
                return FallbackWidth;
            }

            try
            {
                int width = Console.BufferWidth;
                return width > 0 ? width : FallbackWidth;
            }
            catch (Exception)
            {
                return FallbackWidth;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="text">
        /// </param>
        /// <param name="boundary">
        /// </param>
        /// <param name="c">
        /// </param>
        private static void CenteredString(string text, string boundary, ConsoleColor c = ConsoleColor.Black)
        {
            int consoleWidth = SafeWidth();

            int centered = (consoleWidth - text.Length) / 2;
            Console.Write(boundary.PadRight(centered, ' '));

            if (c != ConsoleColor.Black)
            {
                Colouring.Push(c);
            }

            Console.Write(text);
            Colouring.Pop();
            Console.Write(boundary.PadLeft(consoleWidth - (text.Length + centered), ' '));
        }

        #endregion
    }
}