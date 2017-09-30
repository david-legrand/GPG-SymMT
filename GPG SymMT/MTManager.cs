using ConsoleTools;
using GPGToolkit;
using System;
using System.Timers;

namespace GPGSymMT
{
    internal class MTManager
    {
        private GpgManager _gpg;
        private static ProgressBar progress;

        private string _path, _password;
        private bool _isEncryption, _isBench;
        private int _threadsToUse, _nbBench;

        private string AskPassword(bool isEncryption)
        {
            string actionToDo = isEncryption ? "chiffrement" : "déchiffrement";
            Console.Write(string.Format("\rQuel mot de passe utiliser pour le {0} ? ", actionToDo));
            
            string password = Security.GetPassword();
            return password;
        }

        internal MTManager(GpgManager gpg, string path, string password, bool isEncryption, int threadsToUse, bool isBench, int nbBench)
        {
            _gpg = gpg;
            _path = path;
            _password = password;
            _isEncryption = isEncryption;
            _isBench = isBench;
            _threadsToUse = threadsToUse;
            _nbBench = nbBench;
        }

        internal void LaunchProcessing()
        {
            while (string.IsNullOrEmpty(_password)) _password = AskPassword(_isEncryption);
            Tools.CleanLastLine();

            int total = 0;

            for (int i = 0; i < _nbBench; i++)
            {
                int result = 0;
                result = TimedProcessing();
                total += result;
                if (_isBench) ShowResult(i, result);
            }

            if (_isBench) Console.WriteLine(string.Format("\nTests terminés en {0} secondes, soit {1} secondes en moyenne",
                    (total).ToString(),
                    (total / _nbBench).ToString()));
            else Tools.CleanLastLine();

            _gpg.ShowOutputFiles();
            Tools.WaitForTheEnd();
        }

        private int TimedProcessing()
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;

            string actionToDo = _isEncryption ? "Chiffrement" : "Déchiffrement";
            Console.Write(string.Format("\r{0} en cours : ", actionToDo));

            progress = new ProgressBar();
            Timer aTimer = new Timer(500);
            aTimer.Elapsed += OnTimedEvent;
            aTimer.Start();

            int result = _gpg.LaunchProcessing(_path, _password, _isEncryption, _threadsToUse);

            progress.Dispose();
            aTimer.Stop();
            Console.ForegroundColor = oldColor;

            return result;
        }

        private void ShowResult(int testNumber, int result)
        {
            Console.WriteLine(string.Format("\r{0}. Test terminé en {1} secondes", testNumber + 1, result.ToString()));
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            UpdateProgress();
        }

        private void UpdateProgress()
        {
            if (_gpg.progress > 0) progress.Report(_gpg.progress);
        }
    }
}
