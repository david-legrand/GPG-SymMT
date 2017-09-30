using AppsToolkit;
using GPGToolkit;
using System;
using System.IO;

namespace GPGSymMT
{
    class Program
    {
        private static GpgManager gpg = new GpgManager();
        private static ConsoleColor oldColor = Console.ForegroundColor;

        static void Main(string[] args)
        {
            Console.Title = String.Format("{0} v{1}",
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

            try
            {
                gpg.Init(null);
                Console.Title += string.Format(" - {0}", gpg.Version);

                if (gpg.Initialized) CheckArgsAndRun(args);
                else throw new GpgException();
            }
            catch (Exception) { ErrorMessageAndQuit("Erreur : GPG n'a pas été trouvé sur votre système..."); }
        }

        private static void CheckArgsAndRun(string[] args)
        {
            bool isEncryption = true;
            bool isBench = false;

            string password, path;
            password = path = string.Empty;

            int nbBench, nbFiles, threadsToUse, totalSizeFiles;
            nbBench = nbFiles = threadsToUse = totalSizeFiles = 0;
            
            if (args.Length == 0 || args[0] == "-h" || args[0] == "--help") ShowHelp();
            else
            {
                for (int i = 0; i < args.Length; i++)
                {
                    // En premier lieu on traite les éléments à action unique
                    if (args[i] == "-g" || args[i] == "--generate")
                    {
                        if (!int.TryParse(args[i + 1], out nbFiles)) ShowHelpWithError();
                        if (!int.TryParse(args[i + 2], out totalSizeFiles)) ShowHelpWithError();
                        break;
                    }
                    else
                    {
                        path = FilesTools.CleanDirectoryName(args[args.Length - 1]);

                        if (Directory.Exists(path))
                        {
                            if ((args[0] == "-p" || args[0] == "-password")
                            && !args[1].Equals("-b")
                            && !args[1].Equals("-d")
                            && !args[1].Equals("-e")
                            && !args[1].Equals("-g")
                            && !args[1].Equals("-h")
                            && !args[1].Equals("-t")
                            && !args[1].Equals("--bench")
                            && !args[1].Equals("--decrypt")
                            && !args[1].Equals("--encrypt")
                            && !args[1].Equals("--generate")
                            && !args[1].Equals("--threads")
                            && !args[1].Equals("--help")) password = args[1];
                            
                            if (args[i] == "-b" || args[i] == "--bench")
                            {
                                if (!int.TryParse(args[i + 1], out nbBench)) nbBench = 3;
                                isBench = true;
                            }
                            
                            if (args[i] == "-d" || args[i] == "--decrypt") isEncryption = false;
                            if (args[i] == "-t" || args[i] == "--threads")
                            {
                                if (!int.TryParse(args[i + 1], out threadsToUse)) threadsToUse = Environment.ProcessorCount;
                            }
                        }
                        else ErrorMessageAndQuit("Erreur : Aucun répertoire valide n'a été indiqué...");
                    }
                }
                if (nbFiles > 0 && totalSizeFiles > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\rCréation des fichiers en cours...");

                    try { FilesTools.GenerateData(nbFiles, totalSizeFiles); }
                    catch (Exception) { ErrorMessageAndQuit("Erreur : un problème est survenu pendant la création des fichiers..."); }

                    Console.ForegroundColor = oldColor;
                }
                else
                {
                    try
                    {
                        nbBench = ValuesTools.NormalizeInt(nbBench, 1, 99, 1);
                        threadsToUse = ValuesTools.NormalizeInt(threadsToUse, 1, Environment.ProcessorCount, Environment.ProcessorCount);

                        MTManager SymmetricMT = new MTManager(gpg, path, password, isEncryption, threadsToUse, isBench, nbBench);
                        SymmetricMT.LaunchProcessing();
                    }
                    catch (Exception) { ErrorMessageAndQuit("Erreur : un problème est survenu pendant le traitement des fichiers..."); }
                }
            }
        }

        private static void ErrorMessageAndQuit(string message)
        {
            Console.ForegroundColor = oldColor;
            Console.Write(string.Format("\r{0}", message));
            Environment.Exit(-1);
        }

        private static void ShowHelpWithError()
        {
            Console.WriteLine();
            Console.WriteLine(@"/!\ La ligne de commande entrée est invalide, veuillez recommencer...");
            Console.WriteLine();
            ShowHelp();
        }

        private static void ShowHelp()
        {
            Console.WriteLine(String.Format("{0} v{1}",
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Name,
                            System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            Console.WriteLine(gpg.Version);
            Console.WriteLine();
            Console.WriteLine("Copyright(C) Legrand David <me@davlgd.fr>");
            Console.WriteLine("License GPLv3 : GNU GPL version 3 <https://gnu.org/licenses/gpl.html>");
            Console.WriteLine("This is free software: you are free to change and redistribute it.");
            Console.WriteLine("There is NO WARRANTY, to the extent permitted by law.");
            Console.WriteLine();
            Console.WriteLine(string.Format("Syntaxe : {0} [options] [commandes] [dossier]",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name));
            Console.WriteLine("Chiffrez ou déchiffrez l'ensemble des fichiers contenus dans un dossier");
            Console.WriteLine();
            Console.WriteLine("Commandes :");
            Console.WriteLine();
            Console.WriteLine(" -b, --bench [n]                 mesure les performances n fois (défaut : 3, max : 99)");
            Console.WriteLine(" -d, --decrypt                   chiffre les fichiers du dossier");
            Console.WriteLine(" -e, --encrypt                   déchiffre les fichiers du dossier");
            Console.WriteLine(" -g, --generate [n] [size]       génère n fichiers pour une taille définie (en Mo)");
            Console.WriteLine(" -h, --help                      affiche ce message d'aide");
            Console.WriteLine(" -t, --threads [n]               indique le nombre de threads à utiliser");
            Console.WriteLine();
            Console.WriteLine("Options :");
            Console.WriteLine();
            Console.WriteLine(" -p, --passphrase [p]            phrase de passe à utiliser pour le chiffrement");
            Console.WriteLine();
            Console.WriteLine("Si la phrase de passe ou le chemin du dossier contiennent un espace, utilisez des guillemets");
            Console.WriteLine();
            Console.WriteLine(string.Format("Exemple : {0} -p \"Ceci est une phrase de passe\" -e -b 3 \"C:\\Users\\Utilisateur\\Bureau\\Dossier à chiffrer\"",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Name));
            Environment.Exit(0);
        }
    }
}
