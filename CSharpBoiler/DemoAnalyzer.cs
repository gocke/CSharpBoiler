﻿using DemoInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharpBoiler
{
    class DemoAnalyzer
    {
        MatchData mainMatchData;
        string mainSteamID;
        long mainSteamID64Basis;
        int mainPlayerID;
        Player mainPlayer;
        DemoParser demoParser;
        private const long VOLVOMAGICNUMBER = 76561197960265728;

        //Parameters for Start->End calculations
        int K3 = 0;
        int K4 = 0;
        int K5 = 0;
        int matchKills = 0; //matchkills
        int headShots = 0;
        int roundcounter = 0;

        //Parameters for Round->Round calculations
        int roundKills = 0;



        public DemoAnalyzer(MatchData tempMatchData, string tempSteamID)
        {
            mainMatchData = tempMatchData;
            mainSteamID = tempSteamID;

            long tempSteamIDlong;
            long.TryParse(mainSteamID, out tempSteamIDlong);
            //Volvo Magic Number
            mainSteamID64Basis = tempSteamIDlong + VOLVOMAGICNUMBER;
        }

        public async Task<bool> Analyze()
        {
            return await Task.Run(() =>
            {

                string[] tempURLSplit = mainMatchData.Demo.Split('/');
                string tempDemoFileName = tempURLSplit[tempURLSplit.Length - 1];
                string demoName = tempDemoFileName.Substring(0, tempDemoFileName.Length - 4);

                //error Checking
                if (!Directory.Exists("Demos") || !File.Exists("Demos/" + demoName) || demoName.Substring(demoName.Length - 4, 4) != ".dem")
                    return false;

                demoParser = new DemoParser(File.OpenRead("Demos/" + demoName));

                demoParser.MatchStarted += parser_MatchStarted;
                demoParser.PlayerKilled += HandlePlayerKilled;
                demoParser.WeaponFired += HandleWeaponFired;
                demoParser.RoundStart += demoParser_RoundStart;

                demoParser.ParseHeader();

                bool nextTickAvailable = true;
                try
                {
                    while (nextTickAvailable)
                    {
                        nextTickAvailable = demoParser.ParseNextTick();
                        mainMatchData.AnalysisProgress = (int)(demoParser.ParsingProgess * 100);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.StackTrace);
                }

                mainMatchData.K3 = K3;
                mainMatchData.K4 = K4;
                mainMatchData.K5 = K5;
                if (headShots != 0)
                    mainMatchData.HS = (int)((double)headShots / (double)matchKills * 100.0);
                else
                    mainMatchData.HS = 0;

                return true;
            }).ConfigureAwait(continueOnCapturedContext: false);
        }

        //Most Calculation will be done here
        void demoParser_RoundStart(object sender, RoundStartedEventArgs e)
        {
            if (mainPlayer == null)
            {
                foreach (var player in demoParser.PlayingParticipants)
                {
                    if (player != null && player.SteamID == mainSteamID64Basis)
                    {
                        mainPlayerID = player.EntityID;
                        mainPlayer = player;
                    }
                }
            }

            switch(roundKills)
            {
                case 3:
                    K3++;
                    break;
                case 4:
                    K4++;
                    break;
                case 5:
                    K5++;
                    break;
            }

            roundcounter++;
            roundKills = 0;
        }

        private void parser_TickDone(object sender, TickDoneEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void parser_MatchStarted(object sender, MatchStartedEventArgs e)
        {
            //throw new NotImplementedException();
        }

        private void HandlePlayerKilled(object sender, PlayerKilledEventArgs e)
        {
            if (mainPlayer != null && e.Killer == mainPlayer && e.DeathPerson.Team != e.Killer.Team)
            {
                roundKills++;
                matchKills++;
                if(e.Headshot)
                {
                    headShots++;
                }
            }


        }

        private void HandleWeaponFired(object sender, WeaponFiredEventArgs e)
        {
            //throw new NotImplementedException();
        }
    }
}