#nullable enable

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Linq;

namespace Ur.Data
{

    public class GameObject : INotifyPropertyChanged {
        public string Id { get; } = Guid.NewGuid().ToString();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Game : GameObject
    {
        public string Title { get; set; } = "";
        public Player[] Players { get; } = new Player[2] {
            new Player(0),
            new Player(1) { PieceColor = "black" }
        };
        public bool IsActive => !IsPendingPlayer;
        public bool IsPendingPlayer => Players.Any(p => !p.HasUser);
        readonly GamePiece[] pieces;
        public GamePiece[] Pieces => pieces;
        public int ActivePlayerIndex { get; private set; }
        public Player ActivePlayer => Players[ActivePlayerIndex];
        public int RolledPlacesToMove
        {
            get => rolledPlacesToMove;
            private set
            {
                rolledPlacesToMove = value;
                OnPropertyChanged(nameof(RolledPlacesToMove));
            }
        }
        readonly Random random = new Random();
        public DateTimeOffset StartTime { get; } = DateTimeOffset.Now;
        public DateTimeOffset ActiveTime
        {
            get => activeTime;
            private set
            {
                activeTime = value;
                OnPropertyChanged(nameof(ActiveTime));
            }
        }
        public Game()
        {
            pieces = new GamePiece[7 * Players.Length];
            for (int i = 0; i < pieces.Length; i++)
            {
                pieces[i] = new GamePiece(Players[i / 7], i % 7);
            }
            rolledPlacesToMove = random.Next(5);
        }

        string status = "";
        private int rolledPlacesToMove = 1;
        private DateTimeOffset activeTime = DateTimeOffset.Now;

        public string Status
        {
            get => status;
            set
            {
                if (status != value)
                {
                    status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        public async Task MovePieceAsync(GamePiece piece)
        {
            ActiveTime = DateTimeOffset.Now;

            if (piece.Player != Players[ActivePlayerIndex])
            {
                piece.Player.GameMessage = "Not your turn!";
                return;
            }

            var newProgress = piece.Position.GetProgress() + RolledPlacesToMove;
            var newPosition = TilePosition.FromProgress(newProgress, piece.Player) ?? piece.Position;

            if (newProgress == 14) {
                piece.Player.GameMessage = "Score!";
                piece.Position = newPosition;
                piece.IsOut = true;
                piece.Player.Score++;
                await NextTurnAsync ();
                return;
            }
            
            if (newPosition == piece.Position || !CanMoveTo(newPosition, piece.Player))
            {
                piece.Player.GameMessage = "You can't move that piece";
                return;
            }

            var capturedPiece = FindPieceAt(newPosition);
            if (capturedPiece != null && capturedPiece.Player != piece.Player)
            {
                piece.Player.GameMessage = $"Captured {capturedPiece.Color}!";
                capturedPiece.Position = new TilePosition(capturedPiece.Player.OutsideColumn,
                                                            1 + capturedPiece.PieceIndex);
            }
            else {
                piece.Player.GameMessage = "";
            }
            piece.Position = newPosition;

            await NextTurnAsync();
        }

        public bool CanMoveTo(TilePosition position, Player player)
        {
            var otherPiece = FindPieceAt(position);
            return otherPiece switch
            {
                null => true,
                GamePiece p => p.Player != player
            };
        }

        GamePiece? FindPieceAt(TilePosition position)
        {
            return pieces.FirstOrDefault(p => p.Position == position);
        }

        async Task NextTurnAsync()
        {
            ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
            ActivePlayer.GameMessage = "";
            RolledPlacesToMove = random.Next(5);

            while (RolledPlacesToMove == 0)
            {
                ActivePlayer.GameMessage = "Rolled 0, no move!";
                await Task.Delay(3000);
                ActivePlayerIndex = (ActivePlayerIndex + 1) % Players.Length;
                ActivePlayer.GameMessage = "";
                RolledPlacesToMove = random.Next(5);
            }
        }
    }

    public class GamePiece : GameObject {
        public Player Player { get; private set; }
        public int PieceIndex { get; }

        public string Color => Player.PieceColor;
        public TilePosition Position { get; set; }
        public bool IsOut { get; set; }
        public GamePiece(Player player, int pieceIndex)
        {
            Player = player;
            PieceIndex = pieceIndex;
            Position = new TilePosition (
                Color == "white" ? 0 : 4,
                pieceIndex + 1);
        }
    }

    
}