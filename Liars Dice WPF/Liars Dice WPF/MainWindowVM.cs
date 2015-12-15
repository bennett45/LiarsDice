using LDLib;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Liars_Dice_WPF
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        private readonly DelegateCommand addHuman;
        private readonly DelegateCommand addMonte;
        private readonly DelegateCommand addMaybeBluff;
        private readonly DelegateCommand addRandomMonte;
        private readonly DelegateCommand startGame;
        private readonly DelegateCommand call;
        private readonly DelegateCommand guess;
        private readonly DelegateCommand roll;
        private readonly DelegateCommand rollOk;
        private bool hideDice = false;
        private Visibility diceVisibility;

        private int diceValue;
        private int amount;
        
        private const int startingDice = 5;
        private StringBuilder status;
        public bool TellCompDice
        {
            get; set;
        }

        private List<int> dice;
        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowVM()
        {
            dice = new List<int>();
            status = new StringBuilder();
            addHuman = new DelegateCommand(() =>
            {
                var human = new HumanPlayer(startingDice);
                status.Insert(0, string.Format("{0} was added to the game\n", human));
                GameState.Instance.AddPlayer(human);
                Console.WriteLine("I did get hit");
                NotifyPropertyChanged("GameStatus");
                startGame.RaiseCanExecuteChanged();
            });

            addMonte = new DelegateCommand(() =>
            {
                var comp = new Monte(startingDice);
                status.Insert(0, string.Format("{0} was added to the game\n", comp));
                GameState.Instance.AddPlayer(comp);
                NotifyPropertyChanged("GameStatus");
                startGame.RaiseCanExecuteChanged();
            });

            addMaybeBluff = new DelegateCommand(() =>
            {
                var comp = new MaybeBluff(startingDice);
                status.Insert(0, string.Format("{0} was added to the game\n", comp));
                GameState.Instance.AddPlayer(comp);
                NotifyPropertyChanged("GameStatus");
                startGame.RaiseCanExecuteChanged();
            });

            addRandomMonte = new DelegateCommand(() =>
            {
                var comp = new RandomMonte(startingDice);
                status.Insert(0, string.Format("{0} was added to the game\n", comp));
                GameState.Instance.AddPlayer(comp);
                NotifyPropertyChanged("GameStatus");
                startGame.RaiseCanExecuteChanged();
            });

            startGame = new DelegateCommand(() =>
            {
                GameState.Instance.Rolling = true;
                roll.RaiseCanExecuteChanged();
                RollHandler();
            },
            ()=>
            {
                return GameState.Instance.TotalPlayers > 1;
            });

            call = new DelegateCommand(() =>
            {
                status.Insert(0, string.Format("{0} Calls.\n", GameState.Instance.CurrentTurn));
                status.Insert(0, GameState.Instance.GetAllRolls());
                var result = GameState.Instance.EndRound();
                status.Insert(0, string.Format("{0}.\n", result));
                dice = new List<int>();
                NotifyDiceChange();
                NotifyPropertyChanged("GameStatus");
                RoundHandler();
            //},
            //() => 
            //{
            //    return GameState.Instance.ValidMove(new PlayerMove() { Call = true });
            });

            guess = new DelegateCommand(async () =>
            {
                var move = new PlayerMove() { GuessAmount = amount, DiceNumber = diceValue };
                status.Insert(0, string.Format("{0} guesses {1}.\n", GameState.Instance.CurrentTurn, move));
                GameState.Instance.AddMove(move);
                GameState.Instance.EndTurn();
                NotifyPropertyChanged("GameStatus");
                dice = new List<int>();
                NotifyDiceChange();
                NotifyPropertyChanged("LastMove");
                await TurnHandler();
            //},
            //() =>
            //{
            //    return GameState.Instance.ValidMove(new PlayerMove() {DiceNumber = diceValue, GuessAmount = amount });
            });

            roll = new DelegateCommand(() =>
            {
                GameState.Instance.CurrentTurn.Roll();
                dice = GameState.Instance.CurrentTurn.hand;
                NotifyDiceChange();
                rollOk.RaiseCanExecuteChanged();
            },
            () =>
            {
                return GameState.Instance.Rolling;
            });

            rollOk = new DelegateCommand(() =>
            {
                GameState.Instance.EndTurn();
                dice = new List<int>();
                NotifyDiceChange();
                RollHandler();
            },
            () =>
            {
                return dice.Count != 0 && !dice.Contains(0) && GameState.Instance.Rolling; 
            });

            if (hideDice)
                diceVisibility = Visibility.Hidden;
            else
                diceVisibility = Visibility.Visible;
        }

        public string GameStatus
        {
            get { return status.ToString(); }
        }

        public string LastMove
        {
            get { return GameState.Instance.LastMove == null ? "" : GameState.Instance.LastMove.ToString(); }
        }
        
        public string CurrentTurn
        {
            get { return GameState.Instance.CurrentTurn == null ? "" : GameState.Instance.CurrentTurn.ToString(); }

        }
        public DelegateCommand AddHuman
        {
            get { return addHuman; }
        }

        public DelegateCommand AddMonte
        {
            get { return addMonte; }
        }

        public DelegateCommand AddRandomMonte
        {
            get { return addRandomMonte; }
        }

        public DelegateCommand AddMaybeBluff
        {
            get { return addMaybeBluff; }
        }

        public DelegateCommand StartGame
        {
            get { return startGame; }
        }

        public DelegateCommand Call
        {
            get { return call; }
        }

        public DelegateCommand Guess
        {
            get { return guess; }
        }

        public DelegateCommand Roll
        {
            get { return roll; }
        }

        public DelegateCommand RollOk
        {
            get { return rollOk; }
        }

        public int DiceValue
        {
            get { return diceValue; }
            set
            {
                if (value < 7 && value > 0)
                {
                    diceValue = value;
                    guess.RaiseCanExecuteChanged();
                    call.RaiseCanExecuteChanged();
                }
            }
        }

        public int Amount
        {
            get { return amount; }
            set
            {
                if (GameState.Instance.ValidMove(new PlayerMove() { DiceNumber = diceValue, GuessAmount = value }))
                {
                    amount = value;
                    guess.RaiseCanExecuteChanged();
                    call.RaiseCanExecuteChanged();
                }
            }
        }

        public int Dice1
        {
            get { return dice.Count > 0 ? dice[0] : 0; }
            set
            {
                if (dice.Count > 0 && value > 0 && value < 7)
                {
                    dice[0] = value;
                    rollOk.RaiseCanExecuteChanged();
                }
            }
        }

        public int Dice2
        {
            get { return dice.Count > 1 ? dice[1] : 0; }
            set
            {
                if (dice.Count > 1 && value > 0 && value < 7)
                { 
                    dice[1] = value;
                    rollOk.RaiseCanExecuteChanged();
                }
            }
        }

        public int Dice3
        {
            get { return dice.Count > 2 ? dice[2] : 0; }
            set
            {
                if (dice.Count > 2 && value > 0 && value < 7)
                {
                    dice[2] = value;
                    rollOk.RaiseCanExecuteChanged();
                }
            }
        }

        public int Dice4
        {
            get { return dice.Count > 3 ? dice[3] : 0; }
            set
            {
                if (dice.Count > 3 && value > 0 && value < 7)
                { 
                    dice[3] = value;
                    rollOk.RaiseCanExecuteChanged();
                }
            }
        }

        public int Dice5
        {
            get { return dice.Count > 4 ? dice[4] : 0; }
            set
            {
                if (dice.Count > 4 && value > 0 && value < 7)
                { 
                    dice[4] = value;
                    rollOk.RaiseCanExecuteChanged();
                }
            }
        }

        public bool HideDice
        {
            get { return hideDice; }
            set
            {
                hideDice = value;
                diceVisibility = hideDice ? Visibility.Hidden : Visibility.Visible;
                NotifyPropertyChanged("DiceVisibility");
            }
        }

        public Visibility DiceVisibility
        {
            get { return diceVisibility; }
        }

        private void NotifyDiceChange()
        {
            NotifyPropertyChanged("Dice1");
            NotifyPropertyChanged("Dice2");
            NotifyPropertyChanged("Dice3");
            NotifyPropertyChanged("Dice4");
            NotifyPropertyChanged("Dice5");
        }
        protected void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private async Task TurnHandler()
        {
            NotifyPropertyChanged("CurrentTurn");
            call.RaiseCanExecuteChanged();
            var move = await GameState.Instance.Play();
            if (move.Human)
            {
                dice = GameState.Instance.CurrentTurn.hand;
                NotifyDiceChange();
                return;
            }
            if(move.Call)
            {
                await call.Execute();
            }
            else
            {
                status.Insert(0, string.Format("{0} guesses {1}\n", GameState.Instance.CurrentTurn, move));
                GameState.Instance.EndTurn();
                NotifyPropertyChanged("GameStatus");
                await TurnHandler();
            }
        }

        private async void RollHandler()
        {
            var player = GameState.Instance.CurrentTurn;
            if (GameState.Instance.AllPlayersRolled())
            {
                status.Insert(0, "Ready to play\n");
                NotifyPropertyChanged("GameStatus");
                await TurnHandler();
                return;
            }

            status.Insert(0, string.Format("{0}'s turn to roll\n", player));
            NotifyPropertyChanged("GameStatus");
            rollOk.RaiseCanExecuteChanged();
            if (player.Human)
                return;

            player.Roll();
            if (TellCompDice)
            {
                var compRoll = string.Format("{0} rolls ", player);   
                foreach (var die in player.hand)
                {
                    compRoll += string.Format("{0} ", die);
                }
                status.Insert(0, compRoll + '\n');
                NotifyPropertyChanged("GameStatus");
            }
            GameState.Instance.EndTurn();
            
            RollHandler();
        }

        private void RoundHandler()
        {
            if (GameState.Instance.GameOver)
                status.Append("Game over!");
            else
                startGame.Execute();
        }
    }
}
