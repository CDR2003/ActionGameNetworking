using System;
using System.Threading;

namespace SampleGame
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
			Thread.Sleep( 1000 );
            using (var game = new SampleGame())
                game.Run();
        }
    }
#endif
}
