/*
 * Seralyth Menu  Classes/Menu/PCOnGUIMenu.cs
 * A community driven mod menu for Gorilla Tag with over 1000+ mods
 *
 * Copyright (C) 2026  Seralyth Software
 * https://github.com/Seralyth/Seralyth-Menu
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using GorillaLocomotion;
using GorillaNetworking;
using GorillaTag;
using GorillaTag.Cosmetics;
using ExitGames.Client.Photon;
using Photon.Pun;
using Seralyth.Extensions;
using Seralyth.Managers;
using Seralyth.Menu;
using Seralyth.Mods;
using Seralyth.Utilities;
using Valve.Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using static Seralyth.Menu.Main;
using static Seralyth.Utilities.AssetUtilities;

namespace Seralyth.Classes.Menu
{
    public class PCOnGUIMenu : MonoBehaviour
    {
        public static PCOnGUIMenu Instance;
        public static bool IsOpen;

        private Rect guiRect = new Rect(0f, 0f, 700f, 430f);
        private bool showMods = true;
        private bool showPC;
        private bool showPlayers;
        private bool showPlayerColor;
        private bool showTheme;
        private bool showCredits;
        private bool showIcon;
        private bool showShowcases;
        private bool showCosmetics;
        private bool showAI;
        private bool showReview;
        private bool showGUISettings;
        private bool showGames;
        private bool showSupport;
        private bool enableRainbowSnake = PlayerPrefs.GetInt("GUI_RainbowSnake", 1) == 1;
        private bool enableMouseGlow = PlayerPrefs.GetInt("GUI_MouseGlow", 1) == 1;
        private int tooltipStyle = PlayerPrefs.GetInt("GUI_TooltipStyle", 0);
        private string typewriterTarget = "";
        private int typewriterChars;
        private float typewriterTimer;
        private Rect hoveredButtonRect;
        private string hoveredButtonOriginalLabel;
        private static readonly string[] tooltipStyleNames = { "Normal", "Typewriter", "Fade In", "Button Label", "Button Label Typewriter" };
        private bool showWelcome = PlayerPrefs.GetString("SeralythWelcomeVersion", "") != PluginInfo.BuildTimestamp;
        private bool showAICmds;
        private int pcPageNumber;
        private static List<string> aiChatMessages = new List<string>();
        private static List<string> aiMessages = new List<string>();
        private static Vector2 aiChatScrollPosition;
        private static string aiChatInput = "";
        private static string aiInput = "";
        private static bool aiThinking;
        private static float aiThinkingTimer;
        private static int aiChatVersion = -1;
        private static float aiChatContentH;
        private static List<float> aiChatHeights = new List<float>();
        private static int aiVersion = -1;
        private static float aiContentH;
        private static List<float> aiHeights = new List<float>();
        private static bool aiScrollToBottom;
        private bool showSuggestions;
        private const byte MenuStatusByte = 83;
        private static List<MenuStatusEntry> menuStatusList = new List<MenuStatusEntry>();

        private Dictionary<string, List<PlayerMacroStep>> playerMacroStore = new Dictionary<string, List<PlayerMacroStep>>();
        private Dictionary<string, string> playerNotes = new Dictionary<string, string>();
        private Dictionary<string, string> playerLastSeen = new Dictionary<string, string>();
        private Dictionary<string, int> playerRoles = new Dictionary<string, int>();
        private static readonly Color roleFriendColor = new Color(0.3f, 0.9f, 0.4f);
        private static readonly Color roleFoeColor = new Color(0.95f, 0.25f, 0.25f);
        private static readonly string[] roleNames = { "None", "Friend", "Foe" };
        private bool isRecordingPlayerMacro;
        private string recordingPlayerName = "";
        private Color recordingPlayerColor = Color.white;
        private List<PlayerMacroStep> currentRecordingSteps = new List<PlayerMacroStep>();
        private float macroRecordStartTime;
        private float macroLastRecordTime;
        private bool isPlayingPlayerMacro;
        private string playingMacroPlayerName = "";
        private int macroPlayIndex;
        private float macroPlayNextTime;
        private string macroPlaybackTarget = "";
        private string editingNoteFor = "";
        private string noteInputText = "";
        private static string PlayerDataPath => System.IO.Path.Combine(Application.persistentDataPath, "SeralythPlayerData.json");





        private class MenuStatusEntry
        {
            public int actor;
            public string nickname;
            public string tab;
            public bool isOpen;
        }
        private bool showSearch;
        private string hoveredTooltip = "";
        private string searchText = "";
        private int reviewRating;
        private string reviewName = "";
        private string reviewComment = "";
        private string reviewSubmitResult = "";
        private float reviewSubmitTimer;
        private Vector2 reviewScrollPosition;
        private List<ReviewEntry> reviewEntries = new List<ReviewEntry>();
        private static string LocalReviewPath => System.IO.Path.Combine(Application.persistentDataPath, "SeralythReviews.json");

        private struct TrailPoint
        {
            public Vector2 Position;
            public float Time;
            public float Hue;
        }

        private struct PlayerMacroStep
        {
            public float time;
            public Vector3 headPos;
            public Quaternion headRot;
            public Vector3 leftHandPos;
            public Vector3 rightHandPos;
            public bool leftGrab;
            public bool rightGrab;
        }
        private readonly List<TrailPoint> snakeTrail = new List<TrailPoint>();
        private float snakeProgress;
        private Texture2D snakeDot;
        private const int SnakeTrailLength = 20;
        private Texture2D blurGlow;
        private Texture2D roundedCornerTex;
        private const int CornerRadius = 10;
        private Vector2 smoothMousePos;
        private struct GlowBlob
        {
            public Vector2 Position;
            public float Time;
            public float Hue;
            public float Size;
        }
        private readonly List<GlowBlob> glowBlobs = new List<GlowBlob>();
        private float lastBlobTime;
        private string roomInput = "";
        private Vector2 scrollPosition;
        private Vector2 modScrollPosition;
        private Vector2 tourScrollPosition;
        private bool wasdEnabled;
        public float colorR = 1f;
        public float colorG = 1f;
        public float colorB = 1f;
        private float colorHue;
        private float colorSaturation = 1f;
        private float colorBrightness = 1f;
        private Texture2D colorWheelTexture;
        private const int ColorWheelSize = 180;
        private bool colorWheelDragging;
        private float themeWheelHue;
        private float themeWheelSaturation = 1f;
        private float themeWheelBrightness = 1f;
        private bool themeWheelDragging;
        private Texture2D themeBrightnessBar;
        private string customMenuTitle = "Seralyth Remake";
        private bool useCustomMenuTitle;
        private int playerColorTemplateIndex;
        private static readonly string[] playerColorTemplateNames = { "Default", "Neon", "Pastel", "Matte", "Metallic", "Galaxy", "Monochrome" };
        private static readonly Color[][] playerColorTemplatePresets = new Color[][] {
            new Color[] { new Color(0.54f, 0.17f, 0.89f), new Color(1f, 0f, 1f) },
            new Color[] { new Color(0f, 1f, 0.5f), new Color(1f, 0f, 0.5f) },
            new Color[] { new Color(1f, 0.6f, 0.8f), new Color(0.6f, 0.8f, 1f) },
            new Color[] { new Color(0.4f, 0.4f, 0.4f), new Color(0.6f, 0.6f, 0.6f) },
            new Color[] { new Color(0.7f, 0.7f, 0.8f), new Color(0.9f, 0.9f, 1f) },
            new Color[] { new Color(0.2f, 0f, 0.5f), new Color(0.8f, 0f, 1f) },
            new Color[] { Color.black, Color.white }
        };
        private Texture2D brightnessBarTexture;
        private Vector2 playerColorScrollPosition;
        private bool showPlayerColorPresets = true;
        public float buttonSpacingY = 7f;
        private float lastKeyToggle;
        private int selectedModCategory = -1;
        private int currentCategoryIndex = -1;
        private static int lastSyncedThemeType = -1;

        private static string[] modCategoryNames;
        private static int[] modCategoryIndices;
        private Vector2 modCategoryScrollPosition;
        private Vector2 cosmeticScrollPosition;
        private int selectedCosmeticCategory = -1;
        private Vector2 guiScrollPosition;

        private string[] ttb = { "", "", "", "", "", "", "", "", "" };
        private int ttWinner;
        private int ttTurn;
        private int ttScoreX;
        private int ttScoreO;
        private int ttScoreD;
        private int ttDiff;
        private float ttAICooldown;
        private int ttLineA = -1;
        private int ttLineB = -1;
        private Texture2D ttLineTex;
        private bool ttPlayerIsX = true;
        private string ttAISym = "O";
        private string ttPlayerSym = "X";

        private int gameMode;
        private static readonly string[] gameNames = { "Tic Tac Toe", "Wordle", "Block Blast", "Snake", "Connect Four", "Flappy Bird", "Minesweeper", "2048", "Pong", "Simon Says", "Hangman", "Memory Match", "Checkers", "Sudoku", "Tower Defense", "Maze", "Breakout", "MS Hard", "Chinese Checkers", "Tetris", "Solitaire", "Chess", "Whack-a-Mole", "Reaction Test", "Typing Speed", "Catch Objects", "Pacman", "Tank Battle", "Battleship", "Yahtzee", "Color Match", "Pipe Puzzle", "Lights Out", "Nonogram", "Rock Paper Scissors", "Number Guess", "Dice Roll", "Coin Flip", "Blackjack", "Gomoku", "Dots and Boxes", "Checkers 2P", "Sliding Puzzle", "Bulls and Cows", "FreeCell", "Tron", "Bomberman", "Brick Calculator", "Othello", "Rush Hour" };
        private Vector2 gameScrollPosition;
        private bool showGameHelp;
        private static readonly string[] gameHelp = {
            "Place X or O in a row of 3 to win. Play vs AI with 3 difficulty levels.",
            "Guess the 5-letter word in 6 tries. Green=correct, Yellow=wrong spot, Grey=not in word.",
            "Fit falling shapes to fill rows. Complete rows to clear them. Combo bonus for multi-row clears.",
            "Use arrows/WASD to guide the snake. Eat food to grow. Don't hit walls or yourself!",
            "Drop discs into columns. Get 4 in a row (horizontal, vertical, or diagonal) to win.",
            "Click/tap to flap. Avoid pipes and ground. How many can you score?",
            "Click to reveal cells. Numbers show adjacent mines. Flag suspected mines. Clear board to win.",
            "Slide tiles by swiping/clicking. Combine matching numbers to reach 2048!",
            "Mouse or W/S for your paddle. First to 11 wins. Ball speeds up each rally.",
            "Watch the pattern, then repeat it. Sequence grows each round. How far can you get?",
            "Guess the word letter by letter. 6 wrong guesses and the hangman is complete.",
            "Flip cards to find matching pairs. Remember positions! Match all to win.",
            "Move diagonally and jump opponent pieces. King pieces at the back row. Capture all to win.",
            "Fill the 9x9 grid with numbers 1-9. No repeats in rows, columns, or 3x3 boxes.",
            "Defend your base from waves of enemies. Earn gold to buy/upgrade towers.",
            "Navigate the maze from start to finish. Use arrows/WASD. Maze regenerates each game.",
            "Bounce a ball to break bricks. Don't let the ball fall past your paddle!",
            "Hard mode Minesweeper: 16x16 grid with 40 mines. Flag mode toggle included.",
            "Get all your pieces to the opposite corner. Simplified strategy board game.",
            "Rotate and drop falling pieces (tetrominoes). Complete rows to clear them. How high can you stack?",
            "Classic card game: build 4 foundation piles from Ace to King by suit.",
            "Full chess game vs AI. Click piece then destination. AI auto-moves after you.",
            "Click moles when they pop up! Score 10 per whack. 5 lives - miss and lose one.",
            "Wait for the screen to turn green, then click as fast as you can! Measure your reaction time.",
            "Type the displayed words as fast as you can. 60-second test measures your WPM.",
            "Catch falling bananas in your basket! Move with mouse or A/D keys.",
            "Navigate the maze eating dots. Avoid ghosts! 3 lives.",
            "WASD to move your tank. Click to shoot incoming enemies. Don't let them reach you!",
            "Place 5 ships, then fire on the enemy grid. Sink all their ships before they sink yours!",
            "Roll and hold dice to score combinations. Yahtzee=5 of a kind (50pts). Use all 13 categories.",
            "Match the target color using RGB sliders! Submit your color before time runs out.",
            "Click pipes to rotate them. Connect all pipes to solve the puzzle.",
            "Toggle lights and their neighbors. Turn all lights OFF to solve! Fewest moves = best.",
            "Logic puzzle: use row/column clues to fill cells. Left-click=fill, Right-click=mark X.",
            "Pick Rock, Paper, or Scissors. Beat the AI! Track wins, losses, and streaks.",
            "Guess a number between 1 and 100. Get hints: too high or too low. Fewest guesses wins!",
            "Roll 5 dice up to 3 times per turn. Hold dice between rolls. Score combos for points!",
            "Flip a coin! Track your streak of heads or tails in a row. How high can you go?",
            "Beat the dealer to 21 without going over! Hit to draw, Stand to hold. Ace=1 or 11.",
            "Five in a row on a 15x15 grid. Click to place your stone. Beat the AI!",
            "Click edges between dots to claim lines. Complete a box to earn a point and go again.",
            "Two-player checkers on the same board. Take turns moving and jumping opponent pieces.",
            "Slide tiles into empty space to arrange numbers 1-15 in order. Fewest moves = best!",
            "Guess the 4-digit secret code. Bulls = correct digit & position. Cows = correct digit, wrong spot.",
            "Build 4 foundation piles by suit. Move cards between columns. Click stock to draw cards.",
            "Both you and AI leave light trails on a grid. Don't crash into walls or trails!",
            "Place bombs to destroy blocks and avoid enemies. Collect powerups. Clear all enemies!",
            "Count numbers on colored bricks to find safe ones. Avoid hidden bombs! Clear the board.",
            "Place stones to flip opponent pieces to your color. Surround chains to flip them!",
            "Slide cars to clear a path for the red car to exit. Think strategically!"
        };
        private string wdTarget = "";
        private string[] wdGuesses = { "", "", "", "", "", "" };
        private int[,] wdColors = new int[6, 5];
        private int wdRow;
        private string wdInput = "";
        private int wdGuessesWon;
        private int wdGuessesLost;
        private bool wdResultCounted;
        private int wdHintsUsed;
        private string wdHintText;
        private List<int> wdUsedHintIndices = new List<int>();
        private static string GenerateRandomWord()
        {
            string[] consonants = { "b","c","d","f","g","h","j","k","l","m","n","p","r","s","t","v","w","y" };
            string[] vowels = { "a","e","i","o","u" };
            string[] patterns = { "cvccv","vcvcc","cvcvc","cvvcv","vccvc","ccvcc","cvcvc","vcvvc" };
            string pat = patterns[UnityEngine.Random.Range(0, patterns.Length)];
            string word = "";
            foreach (char c in pat)
                word += c == 'c' ? consonants[UnityEngine.Random.Range(0, consonants.Length)] : vowels[UnityEngine.Random.Range(0, vowels.Length)];
            return word;
        }

        private static readonly Vector2Int[][] bbShapeDefs = new Vector2Int[][]
        {
            new[] { new Vector2Int(0,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(1,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(1,2) },
            new[] { new Vector2Int(1,0), new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(2,1) },
            new[] { new Vector2Int(0,1), new Vector2Int(1,1), new Vector2Int(1,0), new Vector2Int(2,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(2,1) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,2), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2) },
            new[] { new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0) },
            new[] { new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3) },
            new[] { new Vector2Int(0,1), new Vector2Int(1,0), new Vector2Int(1,1), new Vector2Int(1,2), new Vector2Int(2,1) },
        };
        private static readonly Color[] bbBlockColors = new Color[]
        {
            new Color(0.95f, 0.3f, 0.3f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.3f, 0.5f, 0.95f),
            new Color(0.95f, 0.7f, 0.2f),
            new Color(0.8f, 0.3f, 0.9f),
            new Color(0.2f, 0.85f, 0.85f),
            new Color(0.95f, 0.95f, 0.3f),
            new Color(0.95f, 0.5f, 0.7f),
        };
        private int[,] bbGrid = new int[8, 8];
        private int[] bbShapeTypes = new int[3];
        private int[] bbShapeColors = new int[3];
        private bool[] bbShapePlaced = new bool[3];
        private int bbSelectedShape = -1;
        private int bbScore;
        private int bbBestScore;
        private bool bbGameActive;
        private bool bbGameOver;
        private float bbClearAnimTime;
        private float bbComboTextTime;
        private string bbComboText = "";
        private float bbPopupTime;
        private int bbPopupScore;
        private float bbPopupX;
        private float bbPopupY;
        private Vector2 bbScrollPosition;
        private bool bbDragging;
        private int bbDragPiece = -1;

        private const int SnakeGridW = 20;
        private const int SnakeGridH = 15;
        private int[,] snakeGrid;
        private List<Vector2Int> snakeBody;
        private Vector2Int snakeDir;
        private Vector2Int snakeFood;
        private int snakeScore;
        private int snakeBestScore;
        private bool snakeGameActive;
        private bool snakeAlive;
        private float snakeMoveTimer;
        private float snakeMoveInterval = 0.25f;
        private bool snakeUseAI;
        private Vector2 snakeScrollPos;
        private List<Vector2Int> snakePath;

        private const int C4Cols = 7;
        private const int C4Rows = 6;
        private int[,] c4Grid;
        private int c4Winner;
        private int c4Turn;
        private int c4Diff;
        private int c4ScoreX;
        private int c4ScoreO;
        private float c4AICooldown;
        private Vector2 c4ScrollPos;

        private float fbBirdY;
        private float fbBirdVel;
        private float fbBirdX;
        private List<float> fbPipeX;
        private List<float> fbPipeGap;
        private int fbScore;
        private int fbBestScore;
        private bool fbGameActive;
        private bool fbAlive;
        private Vector2 fbScrollPos;
        private float fbGroundOffset;

        private static Texture2D[] gameBackgrounds;
        private static bool gameBgsLoaded;
        private static readonly string[] gameBgPaths = {
            "C:\\Users\\kalew\\OneDrive\\Pictures\\image.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\image_8cc3738.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Photo_25_12-16_17_53_26_91.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\IMG_7678.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\file_000000002fe871f79ea7db10eb58a212.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\IMG_0272.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\download.jfif",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-03-10 145740.png"
        };

        private static readonly string[] mmImagePaths = {
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-05-09 104133.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-07-03 165642.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-06-20 151624.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-07-02 160316.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-06-10 031843.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-06-05 191823.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-06-10 035008.png",
            "C:\\Users\\kalew\\OneDrive\\Pictures\\Screenshots\\Screenshot 2026-06-10 035016.png"
        };
        private static Texture2D[] mmTextures;
        private static bool mmTexturesLoaded;
        private static int gameBgLoadIndex;
        private static bool gameBgLoadStarted;

        private int msRows = 10;
        private int msCols = 10;
        private int msMines = 15;
        private int[,] msGrid;
        private bool[,] msRevealed;
        private bool[,] msFlagged;
        private bool msGameOver;
        private bool msWon;
        private int msFlagsLeft;
        private float msTimer;
        private bool msStarted;
        private bool msFlagMode;
        private Vector2 msScrollPos;

        private int[,] g4Grid;
        private int g4Score;
        private int g4Best;
        private bool g4Active;
        private bool g4Won;

        private float pongBallX, pongBallY;
        private float pongBallVX, pongBallVY;
        private float pongPlayerY;
        private float pongEnemyY;
        private int pongPScore, pongEScore;
        private bool pongStarted;
        private float pongFieldW = 400f;
        private float pongFieldH = 260f;

        private int[] simonPattern;
        private int simonPI;
        private int simonLen;
        private int simonPhase;
        private int simonScore;
        private int simonHigh;
        private float simonTimer;
        private bool simonFlash;
        private int simonFlashI;
        private int simonFlashCount;
        private static readonly Color[] simonCols = { new Color(0.9f,0.2f,0.2f), new Color(0.2f,0.7f,0.2f), new Color(0.2f,0.4f,0.9f), new Color(0.9f,0.8f,0.1f) };
        private static readonly string[] simonNames = { "Red", "Green", "Blue", "Yellow" };

        private string hmWord = "";
        private string hmGuesses = "";
        private int hmWrong;
        private bool hmWon;
        private bool hmLost;
        private Vector2 hmScrollPos;

        private int[,] mmGrid;
        private bool[,] mmOpen;
        private bool[,] mmMatched;
        private int mmR1 = -1, mmC1 = -1;
        private int mmR2 = -1, mmC2 = -1;
        private bool mmBusy;
        private int mmPairs;
        private int mmMoves;
        private bool mmDone;
        private float mmTimer;
        private float mmFlipBack;

        private int[,] ckBoard;
        private int ckTurn;
        private int ckSelR = -1, ckSelC = -1;
        private bool ckGameOver;
        private int ckWinner;
        private Vector2 ckScrollPos;
        private static readonly int[] ckDirR = { -1, -1 };
        private static readonly int[] ckDirC = { -1, 1 };

        private int[,] sdkGrid;
        private int[,] sdkSol;
        private bool[,] sdkFixed;
        private int sdkSelR = -1, sdkSelC = -1;
        private int sdkMistakes;
        private Vector2 sdkScrollPos;

        private float[,,] tdMap;
        private float tdTimer;
        private int tdWave;
        private int tdLives;
        private int tdGold;
        private int tdSelTow;
        private bool tdActive;
        private List<Vector4> tdEnemies;
        private List<float> tdEnemyHP;
        private List<float> tdEnemyMaxHP;
        private float tdSpawnTimer;
        private int tdSpawned;
        private int tdWaveEnemies;
        private float tdGoldTimer;
        private int tdSelectedTowerType;
        private List<float[,,]> tdTowers;
        private static readonly float[] tdTowCost = { 50f, 100f, 150f };
        private static readonly float[] tdTowRange = { 60f, 80f, 100f };
        private static readonly float[] tdTowDmg = { 15f, 30f, 50f };
        private static readonly float[] tdTowRate = { 1f, 0.7f, 0.5f };
        private static readonly string[] tdTowNames = { "Blaster ($50)", "Cannon ($100)", "Sniper ($150)" };

        private int mzW = 12;
        private int mzH = 8;
        private int[,] mzWalls;
        private int mzPR, mzPC;
        private int mzER, mzEC;
        private bool mzDone;
        private bool mzGenerated;
        private Vector2 mzScrollPos;

        // Breakout
        private float brPaddleX;
        private float brBallX, brBallY, brBallVX, brBallVY;
        private bool[,] brBricks;
        private int brLives;
        private int brScore;
        private bool brActive;
        private float brFieldW = 400f, brFieldH = 300f;

        // Minesweeper Hard
        private int mshRows = 16;
        private int mshCols = 16;
        private int mshMines = 40;
        private int[,] mshGrid;
        private bool[,] mshRevealed;
        private bool[,] mshFlagged;
        private bool mshGameOver;
        private bool mshWon;
        private int mshFlagsLeft;
        private float mshTimer;
        private bool mshStarted;
        private bool mshFlagMode;
        private Vector2 mshScrollPos;

        // Tetris
        private int[,] tetGrid;
        private int tetPiece, tetRot, tetX, tetY;
        private int tetNext;
        private int tetScore;
        private bool tetActive;
        private float tetTimer;
        private float tetSpeed;
        private static readonly int[][,] tetShapes = new int[][,] {
            new int[,] {{1,1,1,1}},
            new int[,] {{1,1},{1,1}},
            new int[,] {{0,1,0},{1,1,1}},
            new int[,] {{1,0,0},{1,1,1}},
            new int[,] {{0,0,1},{1,1,1}},
            new int[,] {{1,1,0},{0,1,1}},
            new int[,] {{0,1,1},{1,1,0}}
        };
        private static readonly Color[] tetColors = {
            Color.cyan, Color.yellow, Color.magenta, Color.blue,
            new Color(1f,0.5f,0f), Color.red, Color.green
        };

        // Solitaire
        private List<int>[] solColumns;
        private List<bool>[] solColFaceUp;
        private List<int>[] solFoundation;
        private List<int> solStock;
        private int solWaste;
        private bool solWasteActive;
        private List<int> solWastePile;
        private int solSelectedCol = -1;
        private int solSelectedIdx = -1;

        // Chinese Checkers
        private int[,] ccBoard;
        private int ccSelR = -1, ccSelC = -1;
        private int ccMoves;
        private bool ccGameOver;
        private int ccBoardSize = 11;
        private int ccPlayerPieces;
        private int ccAIPieces;

        // Chess
        private int[,] chBoard;
        private int chTurn;
        private int chSelR = -1, chSelC = -1;
        private bool chGameOver;
        private int chWinner;

        // Whack-a-Mole
        private int[,] wamGrid;
        private float wamTimer;
        private float wamSpawnTimer;
        private int wamScore;
        private int wamLives;
        private bool wamActive;
        private float wamMoleTimer;

        // Reaction Test
        private int rtState;
        private float rtTimer;
        private float rtBest;
        private float rtWaitTime;

        // Typing Speed
        private string[] tstWords;
        private string tstCurrentWord;
        private string tstTyped;
        private int tstCorrect;
        private int tstTotal;
        private float tstTimer;
        private bool tstActive;
        private int tstWPM;
        private float tstStartTime;

        // Catch Objects
        private float coBasketX;
        private float coFieldW = 400f, coFieldH = 300f;
        private List<Vector3> coFalling;
        private int coScore;
        private int coLives;
        private bool coActive;
        private float coSpawnTimer;
        private float coSpeed;

        // Pacman
        private int[,] pacMaze;
        private int pacPR, pacPC;
        private int pacDir;
        private int pacScore;
        private bool pacActive;
        private int pacLives;
        private float pacGhostTimer;
        private List<int[]> pacGhosts;
        private List<int> pacGhostDirs;
        private float pacMoveTimer;

        // Tank Battle
        private float tbPX, tbPY;
        private List<Vector4> tbEnemies;
        private List<float> tbEnemyHP;
        private List<Vector3> tbBullets;
        private List<bool> tbBulletPlayer;
        private int tbScore;
        private int tbLives;
        private bool tbActive;
        private float tbSpawnTimer;
        private float tbShootCooldown;

        // Battleship
        private int[,] bsPlayerBoard;
        private int[,] bsEnemyBoard;
        private bool[,] bsPlayerShips;
        private bool[,] bsEnemyShips;
        private int bsPhase;
        private int bsSelR = -1, bsSelC = -1;
        private int bsPlacingShip;
        private bool bsPlacingH;
        private int bsPlayerHits, bsEnemyHits;
        private bool bsGameOver;

        // Yahtzee
        private int[] yzDice;
        private bool[] yzHeld;
        private int yzRerolls;
        private int[] yzScores;
        private bool[] yzUsed;
        private int yzTotal;
        private bool yzGameOver;

        // Color Match
        private Color cmTarget;
        private Color cmPlayer;
        private int cmScore;
        private float cmTimer;
        private bool cmActive;
        private float cmSliderR, cmSliderG, cmSliderB;

        // Pipe Puzzle
        private int[,] ppGrid;
        private int[,] ppRotation;
        private int ppW = 6, ppH = 6;
        private bool ppSolved;

        // Lights Out
        private bool[,] loGrid;
        private int loMoves;
        private int loSize = 5;

        // Nonogram
        private int[,] nnpGrid;
        private int[,] nnpSolution;
        private int nnpW = 5, nnpH = 5;
        private List<int>[] nnpRowClues;
        private List<int>[] nnpColClues;
        private bool nnpSolved;

        // Rock Paper Scissors
        private int rpsPlayerChoice = -1;
        private int rpsAIChoice = -1;
        private int rpsWins, rpsLosses, rpsDraws;
        private int rpsStreak;
        private string rpsResult = "";
        private float rpsAnimTime;

        // Number Guess
        private int ngTarget;
        private int ngGuess;
        private int ngAttempts;
        private int ngBest = -1;
        private string ngHint = "";
        private bool ngWon;
        private string ngInput = "";

        // Dice Roll
        private int[] drDice = new int[5];
        private bool[] drHeld = new bool[5];
        private int drRerolls;
        private int drScore;
        private int drBestScore;
        private bool drRolled;

        // Coin Flip
        private int cfResult = -1;
        private int cfStreak;
        private string cfStreakType = "";
        private int cfTotal;
        private bool cfFlipping;
        private float cfAnimTime;
        private Texture2D cfCoinTex;

        // Blackjack
        private List<int> bjPlayerHand = new List<int>();
        private List<int> bjDealerHand = new List<int>();
        private List<string> bjPlayerLabels = new List<string>();
        private List<string> bjDealerLabels = new List<string>();
        private int bjBet;
        private int bjChips;
        private bool bjDealerHidden;
        private bool bjGameOver;
        private string bjResult = "";
        private bool bjBetting;

        // Gomoku
        private int[,] gmBoard;
        private int gmTurn;
        private int gmWinner;
        private int gmWinR1, gmWinC1, gmWinR2, gmWinC2;
        private Vector2 gmScrollPos;
        private bool gmAIThinking;

        // Dots and Boxes
        private int dbRows = 5;
        private int dbCols = 5;
        private bool[,] dbHoriz;
        private bool[,] dbVert;
        private int[,] dbBoxes;
        private int dbTurn;
        private int dbScore1, dbScore2;
        private bool dbGameOver;

        // Checkers 2P
        private int[,] ck2Board;
        private int ck2Turn;
        private int ck2SelR = -1, ck2SelC = -1;
        private bool ck2GameOver;
        private int ck2Winner;

        // Sliding Puzzle
        private int[,] spGrid;
        private int spSize = 4;
        private int spMoves;
        private bool spSolved;
        private int spBest;

        // Bulls and Cows
        private int[] bucSecret = new int[4];
        private int[] bucGuessArr = new int[4];
        private int bucAttempt;
        private int bucMaxAttempts = 10;
        private int bucBulls, bucCows;
        private bool bucWon;
        private string bucInput = "";
        private List<string> bucHistory = new List<string>();

        // FreeCell
        private List<int>[] fcColumns;
        private List<bool>[] fcColFaceUp;
        private List<int>[] fcFoundation;
        private List<int> fcStock;
        private int fcWaste;
        private bool fcWasteActive;
        private int fcSelectedCol = -1;
        private int fcSelectedIdx = -1;

        // Tron
        private int trSize = 20;
        private int[,] trGrid;
        private int trPR, trPC;
        private int trER, trEC;
        private int trPDir, trEDir;
        private bool trAlive;
        private bool trActive;
        private int trScore;

        // Bomberman
        private int bmSize = 11;
        private int[,] bmGrid;
        private int bmPR, bmPC;
        private int bmBombs;
        private int bmRange;
        private int bmLives;
        private int bmScore;
        private bool bmActive;
        private List<int[]> bmEnemies;
        private float bmEnemyTimer;
        private float bmBombTimer;
        private bool bmBombPlaced;
        private int bmBombR, bmBombC;

        // Brick Calculator
        private int brcSize = 8;
        private int[,] brcGrid;
        private bool[,] brcRevealed;
        private bool[,] brcFlagged;
        private bool brcGameOver;
        private bool brcWon;
        private int brcBombs;
        private int brcFlagsLeft;
        private bool brcFlagMode;

        // Othello
        private int[,] othBoard;
        private int othTurn;
        private int othWinner;
        private bool othGameOver;
        private Vector2 othScrollPos;

        // Rush Hour
        private int rhSize = 6;
        private int[,] rhGrid;
        private int rhCars;
        private int[] rhCarR, rhCarC, rhCarLen, rhCarDir;
        private int rhSelected = -1;
        private int rhMoves;
        private bool rhSolved;

        private static List<string> onlinePlayers = new List<string>();
        private static bool playersInited;
        private int selectedPlayerIndex = -1;
        private int camMode;
        private float videoCamTimer;
        private int playerInfoPage;
        private Camera fpCamera;
        private RenderTexture fpRenderTexture;
        private Camera mirrorCamera;
        private RenderTexture mirrorRenderTexture;
        private Camera portraitCamera;
        private RenderTexture portraitRenderTexture;
        private Texture2D playerPortrait;
        private string playerPortraitName = "";
        private Rect portraitWindowRect = new Rect(710f, 0f, 280f, 320f);
        private Rect cosmeticsWindowRect = new Rect(-290f, 0f, 280f, 320f);
        private Rect mirrorWindowRect = new Rect(710f, 0f, 280f, 320f);
        private Vector2 cosmeticsScrollPos;
        private Texture2D selfPortrait;
        private bool selfPortraitCaptured;
        private GUIStyle fpNameStyle;
        private bool showTour;
        private bool tourComplete;
        private bool showChat;

        private bool showFriends;
        private Vector2 friendsScrollPosition;
        private string selectedFriendKey = "";
        private string friendsChatInput = "";
        private bool friendsNeedsRefresh = true;
        private int tourIndex;
        private Rect tourAnimRect;
        private int tourPrevIndex = -1;
        private float tourAnimTime;
        private const float TourAnimDuration = 0.7f;
        private float tourOverlayX;
        private float tourOverlayY;
        private float tourOverlayTargetX;
        private float tourOverlayTargetY;
        private float tourFingerX;
        private float tourFingerY;
        private float tourFingerTargetX;
        private float tourFingerTargetY;
        private float tourFingerClickTime;
        private static Texture2D tourCursorTex;
        private string[] tourSteps = new[] {
            "Browse mod categories on the left and click one to see its mods",
            "Toggle individual mods on and off from the main list",
            "Click Back to Tabs at the bottom right to return to the main menu",
            "Click Mods in the sidebar to browse and toggle mods by category",
            "Click PC to change your nickname, join rooms, or use WASD fly",
            "Click Players to see who is currently in your room",
            "Click Chat to send and receive messages with other players",
            "Switch to Announce in the Chat tab to view admin announcements (owners/admins only can send)",
            "Click Player Color to change your gorilla's color",
            "Click Theme to customize the GUI background and button colors",
            "Click Credits for links to GitHub and the Discord server",
            "Click Icon to change the color of the menu icon",
            "Click Showcases to watch video showcases",
            "Click Cosmetics to view and toggle your owned cosmetics on and off",
            "Click Suggestions to submit feedback or browse suggestions from other players",
            "Click AI to type mod names to toggle them on and off",
            "The DISCONNECT button at the bottom leaves the current room",
            "The Search button at the top filters mods by name",
            "Use Insert key to toggle the GUI on and off",
            "Drag the title bar to move the GUI window",
            "Use the page buttons to navigate between pages of mods, or enable Page Scrolling for joystick scroll",
            "Click Review to rate the menu and check reviews from other players",
            "Click GUI Settings to toggle visual effects like the rainbow border and mouse glow",
            "Scroll down the sidebar to find Games with Tic Tac Toe and Wordle"
        };
        private Rect[] tourTargets = new[] {
            new Rect(5f, 21f, 155f, 374f),
            new Rect(170f, 80f, 525f, 310f),
            new Rect(540f, 370f, 150f, 25f),
            new Rect(5f, 21f, 150f, 25f),
            new Rect(5f, 48f, 150f, 25f),
            new Rect(5f, 75f, 150f, 25f),
            new Rect(5f, 102f, 150f, 25f),
            new Rect(170f, 21f, 80f, 22f),
            new Rect(5f, 129f, 150f, 25f),
            new Rect(5f, 156f, 150f, 25f),
            new Rect(5f, 183f, 150f, 25f),
            new Rect(5f, 210f, 150f, 25f),
            new Rect(5f, 237f, 150f, 25f),
            new Rect(5f, 264f, 150f, 25f),
            new Rect(5f, 291f, 150f, 25f),
            new Rect(5f, 318f, 150f, 25f),
            new Rect(0f, 400f, 700f, 25f),
            new Rect(600f, 21f, 95f, 25f),
            new Rect(540f, 2f, 155f, 22f),
            new Rect(165f, 2f, 200f, 22f),
            new Rect(180f, 370f, 400f, 25f),
            new Rect(5f, 345f, 150f, 25f),
            new Rect(5f, 372f, 150f, 25f),
            new Rect(5f, 399f, 150f, 25f)
        };

        private List<string> chatMessages = new List<string>();
        private string chatInput = "";
        private Vector2 chatScrollPosition;
        private int prevChatCount;
        private const byte ChatByte = 80;
        private const byte AnnounceByte = 81;
        private const byte AnnounceDeleteByte = 82;
        private const int chatMaxMessages = 100;
        private const int announceMaxMessages = 100;
        private const string AnnounceRoomPropKey = "SeralythAnnc";
        private static string LocalAnnouncePath => System.IO.Path.Combine(Application.persistentDataPath, "SeralythAnnc.json");

        private List<AnnounceEntry> announceData = new List<AnnounceEntry>();
        private string announceInput = "";
        private bool showAnnouncements;
        private int prevAnnounceCount;
        private string cachedAnnounceJson = "";
        private long announceIdCounter;

        private void InitPlayers()
        {
            if (playersInited) return;
            if (NetworkSystem.Instance == null) return;
            playersInited = true;
            LoadPlayerData();
            NetworkSystem.Instance.OnJoinedRoomEvent += () =>
            {
                onlinePlayers.Clear();
                cachedAnnounceJson = "";
                onlinePlayers.Add(NetworkSystem.Instance.LocalPlayer.NickName + " (you)");
                foreach (NetPlayer p in NetworkSystem.Instance.PlayerListOthers)
                {
                    onlinePlayers.Add(p.NickName);
                    UpdateLastSeen(p.NickName);
                }
                UpdateLastSeen(NetworkSystem.Instance.LocalPlayer.NickName);
            };
            NetworkSystem.Instance.OnPlayerJoined += (NetPlayer p) =>
            {
                if (p != NetworkSystem.Instance.LocalPlayer)
                {
                    onlinePlayers.Add(p.NickName);
                    UpdateLastSeen(p.NickName);
                }
            };
            NetworkSystem.Instance.OnPlayerLeft += (NetPlayer p) =>
            {
                onlinePlayers.Remove(p.NickName);
            };
            NetworkSystem.Instance.OnReturnedToSinglePlayer += () =>
            {
                onlinePlayers.Clear();
            };

            if (NetworkSystem.Instance.InRoom)
            {
                onlinePlayers.Clear();
                onlinePlayers.Add(NetworkSystem.Instance.LocalPlayer.NickName + " (you)");
                foreach (NetPlayer p in NetworkSystem.Instance.PlayerListOthers)
                    onlinePlayers.Add(p.NickName);
            }
        }

        public static void Enable()
        {
            IsOpen = true;
            if (Instance != null) Instance.BroadcastMenuStatus();
        }

        public static void Disable()
        {
            IsOpen = false;
            if (Instance != null) Instance.BroadcastMenuStatus();
        }

        private static void BuildCategoryList()
        {
            if (Buttons.buttons == null || Buttons.buttons.Length == 0 || Buttons.buttons[0] == null) return;

            var navButtons = Buttons.buttons[0].Where(b =>
                b != null &&
                !b.buttonText.StartsWith("Exit ") &&
                b.buttonText != "Join Discord" &&
                b.buttonText != "configuration" &&
                !b.label).ToArray();

#if LEGAL || LEGAL_DEBUG
            navButtons = navButtons.Where(nav =>
            {
                string exitText = "Exit " + nav.buttonText;
                int catIdx = -1;
                for (int c = 0; c < Buttons.buttons.Length; c++)
                {
                    if (Buttons.buttons[c] != null && Buttons.buttons[c].Any(b => b != null && b.buttonText == exitText))
                    {
                        catIdx = c;
                        break;
                    }
                }
                if (catIdx < 0) return true;
                return Buttons.buttons[catIdx].Any(b => b != null && (b.legal || b.label));
            }).ToArray();
#endif

            modCategoryNames = new string[navButtons.Length];
            modCategoryIndices = new int[navButtons.Length];
            for (int i = 0; i < navButtons.Length; i++)
            {
                string name = navButtons[i].buttonText;
                modCategoryNames[i] = name;
                string exitText = "Exit " + name;
                int idx = -1;
                for (int c = 0; c < Buttons.buttons.Length; c++)
                {
                    if (Buttons.buttons[c] != null && Buttons.buttons[c].Any(b => b != null && b.buttonText == exitText))
                    {
                        idx = c;
                        break;
                    }
                }
                modCategoryIndices[i] = idx >= 0 ? idx : i;
            }
        }

        private void SyncFromVrTheme()
        {
            if (backgroundColor != null)
                guiBgColor = backgroundColor.GetColor(0);
            if (textColors != null && textColors.Length > 1)
                guiContentColor = textColors[1].GetColor(0);
            if (buttonColors != null && buttonColors.Length > 0)
                guiColorA = buttonColors[0].GetColor(0);
            if (buttonColors != null && buttonColors.Length > 1)
                guiColorB = buttonColors[1].GetColor(0);
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Insert && Time.time - lastKeyToggle > 0.2f)
            {
                lastKeyToggle = Time.time;
                IsOpen = !IsOpen;
            }

            GUI.Label(new Rect(Screen.width / 2f - 75f, 10f, 150f, 25f), "Open GUI = " + (IsOpen ? "On" : "Off"));

            if (!IsOpen) return;

            if (themeType != lastSyncedThemeType && !isRainbowTheme)
            {
                SyncFromVrTheme();
                lastSyncedThemeType = themeType;
            }

            GUI.backgroundColor = guiBgColor;
            GUI.contentColor = guiContentColor;

            guiRect = GUI.Window(9999, guiRect, MainWindowFunction, "");

            if (showPlayers && selectedPlayerIndex >= 0 && selectedPlayerIndex < onlinePlayers.Count)
            {
                portraitWindowRect = new Rect(guiRect.x + guiRect.width + 4f, guiRect.y, portraitWindowRect.width, portraitWindowRect.height);
                portraitWindowRect = GUI.Window(9998, portraitWindowRect, DrawPlayerPortraitWindow, "");

                cosmeticsWindowRect = new Rect(guiRect.x - cosmeticsWindowRect.width - 4f, guiRect.y, cosmeticsWindowRect.width, cosmeticsWindowRect.height);
                cosmeticsWindowRect = GUI.Window(9997, cosmeticsWindowRect, DrawPlayerCosmeticsWindow, "");
            }

            if (showCosmetics)
            {
                mirrorWindowRect = new Rect(guiRect.x + guiRect.width + 4f, guiRect.y, mirrorWindowRect.width, mirrorWindowRect.height);
                mirrorWindowRect = GUI.Window(9996, mirrorWindowRect, DrawMirrorWindow, "");
            }

            if (IsOpen && Event.current.type == EventType.Repaint && enableRainbowSnake)
            {
                if (snakeDot == null)
                {
                    snakeDot = new Texture2D(1, 1);
                    snakeDot.SetPixel(0, 0, Color.white);
                    snakeDot.Apply();
                }

                float speed = 120f;
                snakeProgress += speed * Time.deltaTime;

                float w = guiRect.width;
                float h = guiRect.height;
                float perimeter = 2f * (w + h);
                float pos = snakeProgress % perimeter;

                float x, y;
                if (pos < w)
                {
                    x = pos; y = 0f;
                }
                else if (pos < w + h)
                {
                    x = w; y = pos - w;
                }
                else if (pos < 2f * w + h)
                {
                    x = w - (pos - w - h); y = h;
                }
                else
                {
                    x = 0f; y = h - (pos - 2f * w - h);
                }

                Vector2 head = new Vector2(guiRect.x + x, guiRect.y + y);
                snakeTrail.Add(new TrailPoint { Position = head, Time = Time.realtimeSinceStartup, Hue = (snakeProgress / perimeter) % 1f });
                if (snakeTrail.Count > SnakeTrailLength)
                    snakeTrail.RemoveAt(0);

                Color prevColor2 = GUI.color;
                for (int i = 0; i < snakeTrail.Count; i++)
                {
                    TrailPoint p = snakeTrail[i];
                    float age = Time.realtimeSinceStartup - p.Time;
                    float t = (float)i / snakeTrail.Count;
                    float size = 4f + 8f * t;
                    Color col = Color.HSVToRGB((p.Hue + age * 0.2f) % 1f, 1f, 1f);
                    col.a = 0.3f + 0.7f * t;
                    GUI.color = col;
                    GUI.DrawTexture(new Rect(p.Position.x - size / 2f, p.Position.y - size / 2f, size, size), snakeDot);
                }
                GUI.color = prevColor2;
            }
        }

        private void InitBlurGlow()
        {
            if (blurGlow != null) return;
            int size = 256;
            blurGlow = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = alpha * alpha * 0.35f;
                    blurGlow.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            blurGlow.Apply();
        }

        private void MainWindowFunction(int windowId)
        {
            if (Event.current.type == EventType.Repaint && enableMouseGlow)
            {
                InitBlurGlow();
                Vector2 rawMouse = Event.current.mousePosition;
                smoothMousePos = Vector2.Lerp(smoothMousePos, rawMouse, Time.deltaTime * 15f);

                float now = Time.realtimeSinceStartup;
                if (Vector2.Distance(rawMouse, glowBlobs.Count > 0 ? glowBlobs[glowBlobs.Count - 1].Position : Vector2.zero) > 8f || glowBlobs.Count == 0)
                {
                    glowBlobs.Add(new GlowBlob
                    {
                        Position = smoothMousePos,
                        Time = now,
                        Hue = (now * 0.4f) % 1f,
                        Size = 100f + UnityEngine.Random.Range(40f, 120f)
                    });
                    if (glowBlobs.Count > 50)
                        glowBlobs.RemoveAt(0);
                }

                Color prevGlow = GUI.color;
                for (int i = 0; i < glowBlobs.Count; i++)
                {
                    GlowBlob blob = glowBlobs[i];
                    float age = now - blob.Time;
                    float life = 1.5f;
                    float alpha = Mathf.Clamp01(1f - age / life);
                    if (alpha <= 0f) continue;

                    float grow = 1f + age * 0.8f;
                    float sz = blob.Size * grow;
                    Color col = Color.HSVToRGB((blob.Hue + age * 0.15f) % 1f, 0.6f, 1f);
                    col.a = alpha * 0.25f;
                    GUI.color = col;
                    GUI.DrawTexture(new Rect(blob.Position.x - sz / 2f, blob.Position.y - sz / 2f, sz, sz), blurGlow);
                }
                GUI.color = prevGlow;
            }

            GUI.DragWindow(new Rect(0f, 0f, 10000f, 25f));
            Color prevColor = GUI.color;

            if (showWelcome)
            {
                GUIStyle welcomeTitleStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                welcomeTitleStyle.normal.textColor = guiColorA;
                GUI.Label(new Rect(0f, guiRect.height / 2f - 80f, guiRect.width, 30f), "Welcome to Seralyth Remake", welcomeTitleStyle);

                GUIStyle welcomeSubStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                    richText = true
                };
                GUI.Label(new Rect(50f, guiRect.height / 2f - 40f, guiRect.width - 100f, 40f), "Click the Tour button to learn where everything is,\nor explore on your own!", welcomeSubStyle);

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(guiRect.width / 2f - 75f, guiRect.height / 2f + 20f, 150f, 30f), "Start Tour"))
                {
                    showWelcome = false;
                    PlayerPrefs.SetString("SeralythWelcomeVersion", PluginInfo.BuildTimestamp);
                    PlayerPrefs.Save();
                    tourIndex = 0;
                    tourPrevIndex = -1;
                    tourComplete = false;
                    showTour = true;
                    showMods = true;
                    Rect initTarget = tourTargets[0];
                    tourFingerX = initTarget.x + initTarget.width * 0.5f;
                    tourFingerY = initTarget.y + initTarget.height * 0.5f;
                    tourFingerTargetX = tourFingerX;
                    tourFingerTargetY = tourFingerY;
                    tourFingerClickTime = 0f;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = guiColorB;
                if (GUI.Button(new Rect(guiRect.width / 2f - 75f, guiRect.height / 2f + 60f, 150f, 30f), "Explore"))
                {
                    showWelcome = false;
                    PlayerPrefs.SetString("SeralythWelcomeVersion", PluginInfo.BuildTimestamp);
                    PlayerPrefs.Save();
                    showMods = true;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = guiColorB;

                GUI.color = guiIconColor;
                GUI.DrawTexture(new Rect(guiRect.width / 2f - 25f, guiRect.height / 2f - 120f, 50f, 50f), GetMenuIcon());
                GUI.color = prevColor;
                return;
            }

            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label);
                titleStyle.fontSize = 16;
                titleStyle.richText = true;
            }
            string catName = !Main.disableCategoryDisplay && showMods && currentCategoryIndex >= 0 ? Buttons.categoryNames[currentCategoryIndex] : null;
            string titleExtra = catName != null ? $" <color=#" + ColorUtility.ToHtmlStringRGB(guiColorB) + ">[{catName}]</color>" : "";
            bool connected = PhotonNetwork.IsConnected;
            string connDot = connected ? "<color=#00FF00>\u25CF</color>" : "<color=#FF0000>\u25CF</color>";
            GUIContent titleContent = new GUIContent(connDot + " <color=#" + ColorUtility.ToHtmlStringRGB(guiContentColor) + ">Seralyth Remake</color> <color=#88888888>v" + PluginInfo.Version + "</color> <color=#" + ColorUtility.ToHtmlStringRGB(guiColorA) + ">FPS: " + Mathf.RoundToInt(1f / Time.deltaTime) + "</color>" + titleExtra);
            Vector2 textSize = titleStyle.CalcSize(titleContent);
            GUI.Label(new Rect(165f, 2f, textSize.x, 22f), titleContent, titleStyle);
            GUI.Label(new Rect(guiRect.width - 160f, 2f, 155f, 22f), "Insert - Toggle GUI");
            GUI.color = guiIconColor;
            GUI.DrawTexture(new Rect(165f + textSize.x + 6f, 0f, 24f, 24f), GetMenuIcon());
            GUI.DrawTexture(new Rect(guiRect.width - 80f, guiRect.height / 2f - 25f, 50f, 50f), GetMenuIcon());
            GUI.color = prevColor;
            GUI.Box(new Rect(0f, 0f, 160f, 430f), "");

            bool prevEnabled = GUI.enabled;
            if (showTour) GUI.enabled = false;

            GUI.backgroundColor = showSearch ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(600f, 21f, 95f, 25f), "Search"))
            {
                showSearch = !showSearch;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            if (showSearch)
            {
                GUI.backgroundColor = guiColorB;
                searchText = GUI.TextField(new Rect(380f, 21f, 215f, 25f), searchText);
            }

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(600f, 50f, 95f, 25f), "Discord"))
            {
                Application.OpenURL("https://discord.gg/npJTZAH3cH");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            DrawSidebar();

            GUI.backgroundColor = guiColorB;

            if (showMods)
                DrawModsTab();
            else if (showPC)
                DrawPCTab();
            else if (showPlayers)
                DrawPlayersTab();
            else if (showPlayerColor)
                DrawPlayerColorTab();
            else if (showTheme)
                DrawThemeTab();
            else if (showCredits)
                DrawCreditsTab();
            else if (showAI)
                DrawAITab();
            else if (showSuggestions)
                DrawSuggestionsTab();
            else if (showShowcases)
                DrawShowcasesTab();
            else if (showCosmetics)
                DrawCosmeticsTab();
            else if (showIcon)
                DrawIconTab();
            else if (showChat)
                DrawChatTab();
            else if (showFriends)
                DrawFriendsTab();
            else if (showReview)
                DrawReviewTab();
            else if (showGUISettings)
                DrawGUISettingsTab();
            else if (showGames)
                DrawGamesTab();
            else if (showSupport)
                DrawSupportTab();

            if (!showCosmetics && mirrorCamera != null && VRRig.LocalRig != null && VRRig.LocalRig.headMesh != null)
            {
                Transform head = VRRig.LocalRig.headMesh.transform;
                mirrorCamera.transform.position = head.position + head.forward * 0.05f;
                mirrorCamera.transform.LookAt(head.position);
                if (!mirrorCamera.gameObject.activeSelf) mirrorCamera.gameObject.SetActive(true);
            }

            if (showMods && !showTour)
            {
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(600f, 80f, 95f, 25f), "Tour"))
                {
                    tourIndex = 0;
                    tourPrevIndex = -1;
                    tourComplete = false;
                    showTour = true;
                    showPC = false;
                    showPlayers = false;
                    showChat = false;
                    showTheme = false;
                    showCredits = false;
                    showIcon = false;
                    showShowcases = false;
                    showCosmetics = false;
                    selectedModCategory = -1;
                    currentCategoryIndex = -1;
                    Rect initTarget = tourTargets[0];
                    tourFingerX = initTarget.x + initTarget.width * 0.5f;
                    tourFingerY = initTarget.y + initTarget.height * 0.5f;
                    tourFingerTargetX = tourFingerX;
                    tourFingerTargetY = tourFingerY;
                    tourFingerClickTime = 0f;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = guiColorB;
            }

            if (showTour)
            {
                GUI.enabled = true;
                DrawTourOverlay();
                GUI.enabled = false;
            }

            GUI.enabled = prevEnabled;

            if (GUI.Button(new Rect(0f, 400f, 700f, 25f), "DISCONNECT"))
            {
                PhotonNetwork.Disconnect();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            DrawRoundedCorners(guiRect.width, guiRect.height);
        }

        private Vector2 sidebarScrollPosition;

        private void DrawSidebar()
        {
            if (showMods)
            {
                BuildCategoryList();

                float categoryAreaHeight = 395f - 21f;
                float categoryTotalHeight = modCategoryNames.Length * 27f;

                modCategoryScrollPosition = GUI.BeginScrollView(
                    new Rect(5f, 21f, 155f, categoryAreaHeight),
                    modCategoryScrollPosition,
                    new Rect(0f, 0f, 150f, categoryTotalHeight),
                    false, true);
                for (int i = 0; i < modCategoryNames.Length; i++)
                {
                    float y = i * 27f;
                    GUI.backgroundColor = selectedModCategory == i ? guiColorA : guiColorB;
                    if (GUI.Button(new Rect(0f, y, 150f, 25f), modCategoryNames[i]))
                    {
                        selectedModCategory = i;
                        currentCategoryIndex = -1;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
                GUI.EndScrollView();
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(guiRect.width - 160f, 370f, 150f, 25f), "Back to Tabs"))
                {
                    showMods = false;
                    selectedModCategory = -1;
                    currentCategoryIndex = -1;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = guiColorB;
                return;
            }

            string[] tabNames = {
                "Mods", "PC", "Players", "Friends", "Chat", "Player Color", "Theme",
                "Credits", "Icon", "Showcases", "Cosmetics", "Suggestions",
                "AI", "Review", "GUI Settings", "Games", "Support"
            };
            string[] tabKeys = {
                "Mods", "PC", "Players", "Friends", "Chat", "PlayerColor", "Theme",
                "Credits", "Icon", "Showcases", "Cosmetics", "Suggestions",
                "AI", "Review", "GUISettings", "Games", "Support"
            };

            float areaH = 395f - 21f;
            float totalH = tabNames.Length * 27f;
            sidebarScrollPosition = GUI.BeginScrollView(
                new Rect(0f, 21f, 160f, areaH),
                sidebarScrollPosition,
                new Rect(0f, 0f, 155f, totalH),
                false, true);
            for (int i = 0; i < tabNames.Length; i++)
            {
                float y = i * 27f;
                GUI.backgroundColor = guiColorB;
                if (GUI.Button(new Rect(5f, y, 150f, 25f), tabNames[i]))
                    SelectTab(tabKeys[i]);
            }
            GUI.EndScrollView();
        }

        private bool DrawButton(float x, float y, float w, float h, string text)
        {
            GUI.backgroundColor = guiColorB;
            return GUI.Button(new Rect(x, y, w, h), text);
        }

        public void SelectTab(string tab)
        {
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            showTour = tab == "Tour";
            showMods = tab == "Mods";
            showPC = tab == "PC";
            showPlayers = tab == "Players";
            showChat = tab == "Chat";
            showFriends = tab == "Friends";
            if (showFriends) friendsNeedsRefresh = true;
            showPlayerColor = tab == "PlayerColor";
            showTheme = tab == "Theme";
            showCredits = tab == "Credits";
            showIcon = tab == "Icon";
            showShowcases = tab == "Showcases";
            showCosmetics = tab == "Cosmetics";
            showSuggestions = tab == "Suggestions";
            showAI = tab == "AI";
            showReview = tab == "Review";
            showGUISettings = tab == "GUISettings";
            showGames = tab == "Games";
            showSupport = tab == "Support";
            BroadcastMenuStatus();
        }

        private string GetCurrentTab()
        {
            if (showMods) return "Mods";
            if (showPC) return "PC";
            if (showPlayers) return "Players";
            if (showPlayerColor) return "PlayerColor";
            if (showTheme) return "Theme";
            if (showCredits) return "Credits";
            if (showAI) return "AI";
            if (showReview) return "Review";
            if (showSuggestions) return "Suggestions";
            if (showShowcases) return "Showcases";
            if (showCosmetics) return "Cosmetics";
            if (showIcon) return "Icon";
            if (showChat) return "Chat";
            if (showFriends) return "Friends";
            if (showGUISettings) return "GUISettings";
            if (showGames) return "Games";
            if (showSupport) return "Support";
            return "Mods";
        }

        private void BroadcastMenuStatus()
        {
            if (!PhotonNetwork.InRoom) return;
            string nick = string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName) ? "Unknown" : PhotonNetwork.LocalPlayer.NickName;
            PhotonNetwork.RaiseEvent(MenuStatusByte, new object[] { PhotonNetwork.LocalPlayer.ActorNumber, nick, GetCurrentTab(), IsOpen },
                new Photon.Realtime.RaiseEventOptions { Receivers = Photon.Realtime.ReceiverGroup.All },
                ExitGames.Client.Photon.SendOptions.SendReliable);
        }

        private void DrawModsTab()
        {
            int idx = currentCategoryIndex >= 0 ? currentCategoryIndex : (selectedModCategory >= 0 ? modCategoryIndices[selectedModCategory] : -1);
            if (idx < 0)
            {
                GUI.Label(new Rect(170f, 50f, 300f, 25f), "Select a category from the sidebar");
                return;
            }

            GUI.Label(new Rect(170f, 50f, 300f, 25f), "<b>" + Buttons.categoryNames[idx] + "</b>");


            List<ButtonInfo> list;
            if (!string.IsNullOrEmpty(searchText))
            {
                list = Buttons.buttons.SelectMany(x => x)
                    .Where(b => b.buttonText.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToList();
            }
            else
            {
                string catName = Buttons.categoryNames[idx];
                if (catName == "Favorite Mods")
                    list = StringsToInfos(favorites.ToArray()).ToList();
                else if (catName == "Enabled Mods")
                    list = Buttons.buttons.SelectMany(x => x).Where(b => b.enabled && b.isTogglable).ToList();
                else if (catName == "Quest Mods")
                {
                    var questList = new List<ButtonInfo>();
                    questList.Add(new ButtonInfo { buttonText = "Exit Quests", method = () => Buttons.CurrentCategoryName = "Main", isTogglable = false, toolTip = "Returns you back to the main page.", legal = true });
                    string pColor = "green";
                    questList.Add(new ButtonInfo { buttonText = $"Level: {Quests.playerLevel}", overlapText = $"Level <color=grey>[</color><color={pColor}>{Quests.playerLevel}</color><color=grey>]</color> ({2 - Quests.questsUntilNextLevel}/2)", isTogglable = false, toolTip = "Your quest level goes up every 2 completions. Infinite levels!", legal = true });
                    string dColor = Quests.selectedDifficulty == "Random" ? "white" : Quests.selectedDifficulty == "Easy" ? "green" : Quests.selectedDifficulty == "Medium" ? "yellow" : "red";
                    questList.Add(new ButtonInfo { buttonText = "Change Quest Difficulty", overlapText = $"Change Quest Difficulty <color=grey>[</color><color={dColor}>{Quests.selectedDifficulty}</color><color=grey>]</color>", method = () => Quests.ChangeDifficulty(), enableMethod = () => Quests.ChangeDifficulty(), disableMethod = () => Quests.ChangeDifficulty(false), incremental = true, isTogglable = false, toolTip = "Changes the quest difficulty.", legal = true });
                    if (Quests.activeQuestCheck != null)
                    {
                        questList.Add(new ButtonInfo { buttonText = "Current Quest", overlapText = $"{Quests.activeDifficulty} {Quests.activeQuestName}", isTogglable = false, toolTip = Quests.activeQuestDare, legal = true });
                        questList.Add(new ButtonInfo { buttonText = "Get Hint", method = Quests.GiveHint, isTogglable = false, toolTip = "Get a hint about the current quest.", legal = true });
                    }
                    else
                    {
                        questList.Add(new ButtonInfo { buttonText = "Next Quest...", isTogglable = false, toolTip = $"New quest in {Mathf.Max(0, Mathf.CeilToInt(Quests.nextQuestTime - Time.time))}s", legal = true });
                        if (Quests.lastCompletedName != null)
                        {
                            string lvlMsg = Quests.lastWasLevelUp ? $" <color=green>(LEVEL UP to {Quests.lastCompletedLevel + 1}!)</color>" : $" ({2 - Quests.questsUntilNextLevel}/2 to lvl {Quests.playerLevel + 1})";
                            questList.Add(new ButtonInfo { buttonText = $"Last: {Quests.lastCompletedDifficulty} {Quests.lastCompletedName}{lvlMsg}", overlapText = $"<color=green>QUEST COMPLETE</color> {Quests.lastCompletedDifficulty} <color=yellow>{Quests.lastCompletedName}</color>{lvlMsg}", isTogglable = false, toolTip = "The quest you just completed!", legal = true });
                        }
                    }
                    questList.Add(new ButtonInfo { buttonText = $"Completed: {Quests.completedCount}", overlapText = $"Completed <color=grey>[</color><color=green>{Quests.completedCount}</color><color=grey>]</color>", isTogglable = false, toolTip = "Total quests completed.", legal = true });
                    questList.Add(new ButtonInfo { buttonText = "Reset Quests", method = Quests.ResetAllQuests, isTogglable = false, toolTip = "Resets level, progress, and starts a new quest in 60 seconds.", legal = true });
                    list = questList;
                }
                else if (catName == "Macros")
                {
                    var macroList = new List<ButtonInfo>();
                    macroList.Add(new ButtonInfo { buttonText = "Exit Macros", method = () => Buttons.CurrentCategoryName = "Movement Mods", isTogglable = false, toolTip = "Returns you back to the movement mods.", legal = true });
                    macroList.Add(new ButtonInfo { buttonText = "Record <color=grey>[</color><color=green>T</color><color=grey>]</color>", method = Movement.RecordMacro, toolTip = "Record your macros with your <color=green>left trigger</color>." });
                    macroList.Add(new ButtonInfo { buttonText = "Macro Gun", method = Movement.MacroGun, toolTip = "Record your macros using a <color=green>gun</color>. Grip to aim, trigger to record." });
                    macroList.Add(new ButtonInfo { buttonText = "Reload Macros", method = Movement.LoadMacros, isTogglable = false, toolTip = "Reloads your macros." });

                    if (isRecordingPlayerMacro)
                    {
                        string hexR = Mathf.RoundToInt(recordingPlayerColor.r * 255f).ToString("X2");
                        string hexG = Mathf.RoundToInt(recordingPlayerColor.g * 255f).ToString("X2");
                        string hexB = Mathf.RoundToInt(recordingPlayerColor.b * 255f).ToString("X2");
                        string hex = hexR + hexG + hexB;
                        macroList.Add(new ButtonInfo
                        {
                            buttonText = $"Recording for {recordingPlayerName}...",
                            overlapText = $"<color=#{hex}>Recording [{recordingPlayerName}]</color> ({currentRecordingSteps.Count} steps)",
                            isTogglable = false,
                            toolTip = "Currently recording a macro. Press Stop Recording to finish.",
                            legal = true
                        });
                        macroList.Add(new ButtonInfo
                        {
                            buttonText = "Stop Recording",
                            isTogglable = false,
                            method = StopPlayerMacroRecording,
                            toolTip = "Stops recording and saves the macro.",
                            legal = true
                        });
                    }
                    else if (isPlayingPlayerMacro)
                    {
                        macroList.Add(new ButtonInfo
                        {
                            buttonText = $"Playing macro for {playingMacroPlayerName}...",
                            overlapText = $"<color=yellow>Playing [{playingMacroPlayerName}]</color> ({macroPlayIndex}/{GetStoredMacroCount(playingMacroPlayerName)})",
                            isTogglable = false,
                            toolTip = "Currently playing a macro.",
                            legal = true
                        });
                        macroList.Add(new ButtonInfo
                        {
                            buttonText = "Stop Playback",
                            isTogglable = false,
                            method = StopPlayerMacroPlayback,
                            toolTip = "Stops the macro playback.",
                            legal = true
                        });
                    }

                    IReadOnlyList<VRRig> allRigs = VRRigCache.ActiveRigs;
                    if (allRigs != null)
                    {
                        for (int r = 0; r < allRigs.Count; r++)
                        {
                            VRRig rig = allRigs[r];
                            if (rig == null || rig.isLocal) continue;

                            string pName = rig.GetName();
                            if (string.IsNullOrEmpty(pName) || pName == "null") continue;
                            Color pc = rig.mainSkin != null ? rig.mainSkin.material.color : rig.playerColor;
                            string hexR2 = Mathf.RoundToInt(pc.r * 255f).ToString("X2");
                            string hexG2 = Mathf.RoundToInt(pc.g * 255f).ToString("X2");
                            string hexB2 = Mathf.RoundToInt(pc.b * 255f).ToString("X2");
                            string pHex = hexR2 + hexG2 + hexB2;

                            int capturedR = r;
                            bool hasMacro = playerMacroStore.ContainsKey(pName) && playerMacroStore[pName].Count > 0;

                            if (isRecordingPlayerMacro && recordingPlayerName == pName)
                            {
                                macroList.Add(new ButtonInfo
                                {
                                    buttonText = $"Record {pName} Macro",
                                    overlapText = $"<color=#{pHex}>Record {pName} Macro</color> <color=red>[REC]</color>",
                                    isTogglable = false,
                                    toolTip = $"Currently recording for {pName}.",
                                    legal = true
                                });
                            }
                            else
                            {
                                string status = hasMacro ? $" <color=green>[{playerMacroStore[pName].Count}]</color>" : "";
                                macroList.Add(new ButtonInfo
                                {
                                    buttonText = $"Record {pName} Macro",
                                    overlapText = $"<color=#{pHex}>Record {pName} Macro</color>{status}",
                                    isTogglable = false,
                                    method = () => StartPlayerMacroRecording(capturedR),
                                    toolTip = hasMacro
                                        ? $"Record a new macro for {pName}. Right-click to play existing ({playerMacroStore[pName].Count} steps)."
                                        : $"Record a macro for {pName}.",
                                    legal = true
                                });
                            }

                            if (hasMacro && !isRecordingPlayerMacro && !isPlayingPlayerMacro)
                            {
                                macroList.Add(new ButtonInfo
                                {
                                    buttonText = $"Play {pName} Macro",
                                    overlapText = $"<color=#{pHex}>Play {pName} Macro</color>",
                                    isTogglable = false,
                                    method = () => StartPlayerMacroPlayback(capturedR),
                                    toolTip = $"Play the recorded macro for {pName}.",
                                    legal = true
                                });
                            }
                        }
                    }

                    if (playerMacroStore.Count > 0 && !isRecordingPlayerMacro && !isPlayingPlayerMacro)
                    {
                        macroList.Add(new ButtonInfo
                        {
                            buttonText = "Clear All Macros",
                            isTogglable = false,
                            method = ClearAllPlayerMacros,
                            toolTip = "Deletes all recorded player macros.",
                            legal = true
                        });
                    }

                    list = macroList;
                }
                else if (catName == "Achievements")
                {
                    AchievementManager.EnterAchievementTab();
                    list = Buttons.buttons[idx].ToList();
                }
                else if (catName == "Friends")
                {
            if (friendsNeedsRefresh)
            {
                friendsNeedsRefresh = false;
                FriendManager.FriendsListUpdated();
            }
                    list = Buttons.buttons[idx].ToList();
                }
                else
                    list = Buttons.buttons[idx].ToList();
            }

#if LEGAL || LEGAL_DEBUG
            list = list.Where(b => b.legal || b.label).ToList();
#endif

            float rowHeight = 25f;
            float startY = 75f;
            float colWidth = 340f;
            int pcFullCount = list.Count;
            int pcPageSize = 12;
            int pcTotalPages = Mathf.Max(1, Mathf.CeilToInt((float)pcFullCount / pcPageSize));
            bool showPageButtons = !pageScrolling && pcTotalPages > 1;
            float scrollHeight = showPageButtons ? guiRect.height - startY - 85f : guiRect.height - startY - 55f;

            if (!pageScrolling)
            {
                pcPageNumber = Mathf.Clamp(pcPageNumber, 0, pcTotalPages - 1);
                list = list.Skip(pcPageNumber * pcPageSize).Take(pcPageSize).ToList();
            }

            hoveredTooltip = "";
            float totalHeight = list.Count * (rowHeight + buttonSpacingY) + 20f;
            modScrollPosition = GUI.BeginScrollView(
                new Rect(170f, startY, guiRect.width - 180f, scrollHeight),
                modScrollPosition,
                new Rect(0f, 0f, colWidth, totalHeight),
                false, true);

            int drawn = 0;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].buttonText == "configuration") continue;

                float y = drawn * (rowHeight + buttonSpacingY);
                bool isFav = favorites.Contains(list[i].buttonText);
                string buttonLabel = string.IsNullOrEmpty(list[i].overlapText) ? list[i].buttonText : list[i].overlapText;

                if (list[i].label)
                {
                    GUI.Label(new Rect(0f, y, colWidth - 40f, rowHeight), buttonLabel);
                    drawn++;
                    continue;
                }

                float mainBtnWidth = list[i].incremental ? colWidth - 110f : colWidth - 40f;

                GUI.backgroundColor = list[i].enabled ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(0f, y, mainBtnWidth, rowHeight), buttonLabel))
                {
                    if (list[i].buttonText.StartsWith("Exit "))
                    {
                        string parentName = list[i].buttonText.Substring(5);
                        int parentIdx = Buttons.GetCategory(parentName);
                        if (parentIdx >= 0)
                        {
                            int sidebarIdx = Array.IndexOf(modCategoryIndices, parentIdx);
                            if (sidebarIdx >= 0)
                            {
                                selectedModCategory = sidebarIdx;
                                currentCategoryIndex = -1;
                            }
                            else
                            {
                                currentCategoryIndex = parentIdx;
                            }
                        }
                    }
                    else
                    {
                        int catIdx = Buttons.GetCategory(list[i].buttonText);
                        if (catIdx >= 0)
                        {
                            currentCategoryIndex = catIdx;
                        }
                        else if (list[i].incremental)
                        {
                            ToggleIncremental(list[i].buttonText, true);
                        }
                        else
                        {
                            Toggle(list[i]);
                        }
                    }
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }

                if (list[i].incremental)
                {
                    GUI.backgroundColor = guiColorB;
                    if (GUI.Button(new Rect(colWidth - 110f, y, 35f, rowHeight), "-"))
                    {
                        ToggleIncremental(list[i].buttonText, false);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    if (GUI.Button(new Rect(colWidth - 75f, y, 35f, rowHeight), "+"))
                    {
                        ToggleIncremental(list[i].buttonText, true);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }

                if (Event.current.type == EventType.Repaint)
                {
                    Rect btnRect = new Rect(0f, y, mainBtnWidth, rowHeight);
                    if (btnRect.Contains(Event.current.mousePosition))
                    {
                        hoveredTooltip = list[i].toolTip;
                        hoveredButtonRect = new Rect(170f, startY + y - modScrollPosition.y, mainBtnWidth, rowHeight);
                        hoveredButtonOriginalLabel = buttonLabel;
                    }
                }

                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = isFav ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(colWidth - 35f, y, 35f, rowHeight), isFav ? "★" : "☆"))
                {
                    if (favorites.Contains(list[i].buttonText))
                        favorites.Remove(list[i].buttonText);
                    else
                        favorites.Add(list[i].buttonText);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = prevBg;
                drawn++;
            }
                GUI.EndScrollView();

                if (showPageButtons)
                {
                    float btnY = 370f;
                    GUI.backgroundColor = guiColorB;
                    GUI.enabled = pcPageNumber > 0;
                    if (GUI.Button(new Rect(180f, btnY, 50f, 25f), "< Prev"))
                    {
                        pcPageNumber--;
                        modScrollPosition = Vector2.zero;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.enabled = true;
                    int maxVisible = Mathf.Min(pcTotalPages, 10);
                    int halfWindow = maxVisible / 2;
                    int windowStart = Mathf.Clamp(pcPageNumber - halfWindow, 0, Mathf.Max(0, pcTotalPages - maxVisible));
                    float pageX = 235f;
                    for (int v = 0; v < maxVisible; v++)
                    {
                        int p = windowStart + v;
                        GUI.backgroundColor = p == pcPageNumber ? guiColorA : guiColorB;
                        int page = p;
                        if (GUI.Button(new Rect(pageX, btnY, 25f, 25f), (page + 1).ToString()))
                        {
                            pcPageNumber = page;
                            modScrollPosition = Vector2.zero;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                        pageX += 27f;
                    }
                    GUI.backgroundColor = guiColorB;
                    GUI.enabled = pcPageNumber < pcTotalPages - 1;
                    if (GUI.Button(new Rect(pageX + 2f, btnY, 50f, 25f), "Next >"))
                    {
                        pcPageNumber++;
                        modScrollPosition = Vector2.zero;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.enabled = true;
                }

                if (!string.IsNullOrEmpty(hoveredTooltip))
                {
                    if (tooltipStyle == 3)
                    {
                        GUIStyle labelStyle = new GUIStyle(GUI.skin.button);
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        labelStyle.fontSize = 12;
                        labelStyle.wordWrap = false;
                        labelStyle.normal.textColor = Color.white;
                        labelStyle.normal.background = GUI.skin.box.normal.background;
                        GUI.Label(hoveredButtonRect, hoveredTooltip, labelStyle);
                    }
                    else if (tooltipStyle == 4)
                    {
                        if (hoveredTooltip != typewriterTarget)
                        {
                            typewriterTarget = hoveredTooltip;
                            typewriterChars = 0;
                            typewriterTimer = Time.realtimeSinceStartup;
                        }

                        float charsPerSec = 30f;
                        typewriterChars = Mathf.Min((int)((Time.realtimeSinceStartup - typewriterTimer) * charsPerSec), typewriterTarget.Length);
                        string displayText = typewriterTarget.Substring(0, typewriterChars);
                        if (typewriterChars < typewriterTarget.Length)
                            displayText += "\u2588";

                        GUIStyle labelStyle = new GUIStyle(GUI.skin.button);
                        labelStyle.alignment = TextAnchor.MiddleCenter;
                        labelStyle.fontSize = 12;
                        labelStyle.wordWrap = false;
                        labelStyle.normal.textColor = Color.white;
                        labelStyle.normal.background = GUI.skin.box.normal.background;
                        GUI.Label(hoveredButtonRect, displayText, labelStyle);
                    }
                    else
                    {
                    Vector2 mousePos = Event.current.mousePosition;
                    GUIStyle ttStyle = new GUIStyle(GUI.skin.box);
                    ttStyle.alignment = TextAnchor.MiddleLeft;
                    ttStyle.fontSize = 11;
                    ttStyle.padding = new RectOffset(6, 6, 4, 4);
                    ttStyle.wordWrap = false;

                    string displayText = hoveredTooltip;
                    float alpha = 1f;

                    if (tooltipStyle != 0)
                    {
                        if (hoveredTooltip != typewriterTarget)
                        {
                            typewriterTarget = hoveredTooltip;
                            typewriterChars = 0;
                            typewriterTimer = Time.realtimeSinceStartup;
                        }

                        if (tooltipStyle == 1)
                        {
                            float charsPerSec = 30f;
                            typewriterChars = Mathf.Min((int)((Time.realtimeSinceStartup - typewriterTimer) * charsPerSec), typewriterTarget.Length);
                            displayText = typewriterTarget.Substring(0, typewriterChars);
                            if (typewriterChars < typewriterTarget.Length)
                                displayText += "\u2588";
                        }
                        else if (tooltipStyle == 2)
                        {
                            float fadeDur = 0.4f;
                            alpha = Mathf.Clamp01((Time.realtimeSinceStartup - typewriterTimer) / fadeDur);
                        }
                    }

                    ttStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
                    GUIContent tc = new GUIContent(displayText);
                    Vector2 size = ttStyle.CalcSize(tc);
                    float tx = mousePos.x + 15f;
                    float ty = mousePos.y + 15f;
                    if (tx + size.x > guiRect.width) tx = guiRect.width - size.x - 5f;
                    if (ty + size.y > guiRect.height) ty = mousePos.y - size.y - 5f;
                    GUI.Box(new Rect(tx, ty, size.x, size.y), tc, ttStyle);
                    }
                }
        }

        private void DrawPCTab()
        {
            guiScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 21f, guiRect.width - 170f, guiRect.height - 56f),
                guiScrollPosition,
                new Rect(0f, 0f, 500f, 360f),
                false, true);

            roomInput = GUI.TextField(new Rect(0f, 0f, 300f, 30f), roomInput);

            if (GUI.Button(new Rect(0f, 34f, 300f, 30f), "Set Name"))
            {
                PhotonNetwork.LocalPlayer.NickName = roomInput;
                PhotonNetwork.NickName = roomInput;
                PlayerPrefs.SetString("GTPlayerName", roomInput);
                GorillaComputer.instance.currentName = roomInput;
                GorillaComputer.instance.name = roomInput;
                PlayerPrefs.Save();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            if (GUI.Button(new Rect(0f, 68f, 300f, 30f), "Join Room"))
            {
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomInput, JoinType.Solo);
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            GUI.Label(new Rect(0f, 102f, 300f, 30f), "WASD Speed = " + wasdSpeed.ToString("F2"));
            wasdSpeed = GUI.HorizontalSlider(new Rect(0f, 124f, 300f, 30f), wasdSpeed, 0f, 10f);
            GUI.Label(new Rect(0f, 154f, 300f, 30f), "WASD Rotation = " + wasdRotation.ToString("F2"));
            wasdRotation = GUI.HorizontalSlider(new Rect(0f, 176f, 300f, 30f), wasdRotation, 0f, 10f);
            GUI.Label(new Rect(0f, 206f, 300f, 30f), "WASD Jump = " + wasdJump.ToString("F2"));
            wasdJump = GUI.HorizontalSlider(new Rect(0f, 228f, 300f, 30f), wasdJump, 0f, 10f);

            GUI.backgroundColor = wasdEnabled ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(0f, 259f, 300f, 30f), "WASD Fly"))
            {
                wasdEnabled = !wasdEnabled;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            GUI.EndScrollView();
        }

        private int prevCategoryStep = -1;

        private void DrawTourOverlay()
        {
            int[] sidebarTourTabIndices = { -1, -1, -1, 0, 1, 2, 3, -1, 4, 5, 6, 7, 8, 9, 10, 11, -1, -1, -1, -1, -1, 12, 13, 14 };
            int sidebarTab = (tourIndex >= 0 && tourIndex < sidebarTourTabIndices.Length) ? sidebarTourTabIndices[tourIndex] : -1;
            Rect target;
            if (sidebarTab >= 0)
                target = new Rect(5f, 21f + sidebarTab * 27f - sidebarScrollPosition.y, 150f, 25f);
            else
                target = tourTargets[tourIndex];
            float overlayW = 320f;
            float overlayH = 190f;

            if (tourPrevIndex != tourIndex)
            {
                if (tourIndex >= 0 && tourIndex < sidebarTourTabIndices.Length)
                {
                    int tabIdx = sidebarTourTabIndices[tourIndex];
                    if (tabIdx >= 0)
                    {
                        float tabY = tabIdx * 27f;
                        float areaH = 374f;
                        if (tabY < sidebarScrollPosition.y)
                            sidebarScrollPosition.y = tabY;
                        else if (tabY + 25f > sidebarScrollPosition.y + areaH)
                            sidebarScrollPosition.y = tabY + 25f - areaH;
                    }
                }
                float maxOY = 355f - overlayH;

                float CalcOx(Rect t)
                {
                    if (t.x < 160f)
                        return 165f;
                    return Mathf.Clamp(t.x + t.width + 10f, 0f, 700f - overlayW);
                }
                float CalcOy(Rect t)
                {
                    if (t.x < 160f)
                        return Mathf.Clamp(t.y - 20f, 0f, maxOY);
                    if (t.y + t.height + overlayH > 380f)
                        return Mathf.Clamp(t.y - overlayH - 10f, 0f, maxOY);
                    return Mathf.Clamp(t.y + t.height + 10f, 0f, maxOY);
                }

                if (tourPrevIndex < 0)
                {
                    tourAnimRect = target;
                    tourOverlayX = CalcOx(target);
                    tourOverlayY = CalcOy(target);
                    tourOverlayTargetX = tourOverlayX;
                    tourOverlayTargetY = tourOverlayY;
                }
                else
                {
                    tourAnimRect = new Rect(tourAnimRect.x, tourAnimRect.y, tourAnimRect.width, tourAnimRect.height);
                    tourOverlayTargetX = CalcOx(target);
                    tourOverlayTargetY = CalcOy(target);
                }
                tourFingerTargetX = target.x + target.width * 0.5f;
                tourFingerTargetY = target.y + target.height * 0.5f;
                tourFingerClickTime = 0f;
                tourAnimTime = 0f;
                tourPrevIndex = tourIndex;
            }

            tourAnimTime += Time.deltaTime;
            float t = Mathf.Clamp01(tourAnimTime / TourAnimDuration);
            float smooth = t * t * (3f - 2f * t);

            Rect animHighlight = new Rect(
                Mathf.Lerp(tourAnimRect.x, target.x, smooth),
                Mathf.Lerp(tourAnimRect.y, target.y, smooth),
                Mathf.Lerp(tourAnimRect.width, target.width, smooth),
                Mathf.Lerp(tourAnimRect.height, target.height, smooth));

            tourAnimRect = animHighlight;

            float ox = Mathf.Lerp(tourOverlayX, tourOverlayTargetX, smooth);
            float oy = Mathf.Lerp(tourOverlayY, tourOverlayTargetY, smooth);
            tourOverlayX = ox;
            tourOverlayY = oy;

            bool onCategoryStep = tourIndex == 0 || tourIndex == 1 || tourIndex == 2;
            bool onChatStep = tourIndex == 6 || tourIndex == 7;
            bool onAIStep = tourIndex == 15;
            bool onPageStep = tourIndex == 20;
            bool onGUISettingsStep = tourIndex == 22;
            if (onPageStep && prevCategoryStep != tourIndex)
            {
                showMods = true;
                showPC = false;
                showPlayers = false;
                showChat = false;
                showPlayerColor = false;
                showTheme = false;
                showCredits = false;
                showIcon = false;
                showShowcases = false;
                showSuggestions = false;
                showAI = false;
                showGUISettings = false;
                pcPageNumber = 0;
                pageScrolling = false;
                modScrollPosition = Vector2.zero;
                int mainIdx = Buttons.categoryNames != null ? Array.IndexOf(Buttons.categoryNames, "Fun") : -1;
                if (mainIdx >= 0)
                {
                    int sidebarIdx = Array.IndexOf(modCategoryIndices, mainIdx);
                    if (sidebarIdx >= 0)
                    {
                        selectedModCategory = sidebarIdx;
                        currentCategoryIndex = -1;
                    }
                    else
                    {
                        currentCategoryIndex = mainIdx;
                        selectedModCategory = -1;
                    }
                }
            }
            else if (onCategoryStep && prevCategoryStep != tourIndex)
            {
                showMods = true;
                showPC = false;
                showPlayers = false;
                showChat = false;
                showPlayerColor = false;
                showTheme = false;
                showCredits = false;
                showIcon = false;
                showShowcases = false;
                showSuggestions = false;
                showAI = false;
                showGUISettings = false;
                selectedModCategory = -1;
                currentCategoryIndex = -1;
            }
            else if (onAIStep && prevCategoryStep != tourIndex)
            {
                showMods = false;
                showPC = false;
                showPlayers = false;
                showChat = false;
                showPlayerColor = false;
                showTheme = false;
                showCredits = false;
                showIcon = false;
                showShowcases = false;
                showSuggestions = false;
                showAI = true;
                showGUISettings = false;
                selectedModCategory = -1;
                currentCategoryIndex = -1;
            }
            else if (onChatStep && prevCategoryStep != tourIndex)
            {
                showMods = false;
                showPC = false;
                showPlayers = false;
                showChat = true;
                showPlayerColor = false;
                showTheme = false;
                showCredits = false;
                showIcon = false;
                showShowcases = false;
                showSuggestions = false;
                showAI = false;
                showGUISettings = false;
                selectedModCategory = -1;
                currentCategoryIndex = -1;
            }
            else if (onGUISettingsStep && prevCategoryStep != tourIndex)
            {
                showMods = false;
                showPC = false;
                showPlayers = false;
                showChat = false;
                showPlayerColor = false;
                showTheme = false;
                showCredits = false;
                showIcon = false;
                showShowcases = false;
                showSuggestions = false;
                showAI = false;
                showGUISettings = true;
                selectedModCategory = -1;
                currentCategoryIndex = -1;
            }
            else if (!onCategoryStep && !onChatStep && !onAIStep && !onGUISettingsStep && prevCategoryStep >= 0)
            {
                showMods = false;
                showChat = false;
                showAI = false;
                showGUISettings = false;
            }
            prevCategoryStep = onCategoryStep || onChatStep || onAIStep || onPageStep || onGUISettingsStep ? tourIndex : -1;

            Color prev = GUI.color;
            GUI.Box(new Rect(ox, oy, overlayW, overlayH), "");
            GUI.backgroundColor = guiColorA;
            GUI.Box(new Rect(ox + 10f, oy + 10f, overlayW - 20f, 25f), "Tour Guide");
            GUI.backgroundColor = guiColorB;

            if (tourComplete)
            {
                GUIStyle bigStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                bigStyle.normal.textColor = guiColorA;
                GUI.Label(new Rect(ox + 15f, oy + 60f, overlayW - 30f, 50f), "You're all set!", bigStyle);

                GUIStyle subStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true
                };
                GUI.Label(new Rect(ox + 15f, oy + 100f, overlayW - 30f, 40f), "You now know your way around Seralyth Remake. Enjoy!", subStyle);

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(ox + 20f, oy + overlayH - 32f, overlayW - 40f, 22f), "Done"))
                {
                    showTour = false;
                    tourComplete = false;
                    showMods = true;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = guiColorB;

                DrawTourHighlight(animHighlight);
                GUI.color = prev;
                return;
            }

            float textAreaH = 60f;
            tourScrollPosition = GUI.BeginScrollView(
                new Rect(ox + 15f, oy + 45f, overlayW - 30f, textAreaH),
                tourScrollPosition,
                new Rect(0f, 0f, overlayW - 50f, 100f), false, true);
            GUI.Label(new Rect(0f, 0f, overlayW - 50f, 100f), tourSteps[tourIndex]);
            GUI.EndScrollView();

            string progress = "Step " + (tourIndex + 1) + " of " + tourSteps.Length;
            GUI.Label(new Rect(ox + 20f, oy + 110f, overlayW - 40f, 20f), progress);

            GUI.enabled = tourIndex > 0;
            if (GUI.Button(new Rect(ox + 20f, oy + 135f, 130f, 25f), "< Previous"))
            {
                tourIndex = (tourIndex - 1 + tourSteps.Length) % tourSteps.Length;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(ox + overlayW - 150f, oy + 135f, 130f, 25f), tourIndex >= tourSteps.Length - 1 ? "Finish" : "Next >"))
            {
                if (tourIndex >= tourSteps.Length - 1)
                {
                    tourComplete = true;
                }
                else
                {
                    tourIndex++;
                }
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = Color.red;
            if (GUI.Button(new Rect(ox + overlayW - 140f, oy + overlayH - 32f, 120f, 22f), "Done"))
            {
                showTour = false;
                showMods = false;
                selectedModCategory = -1;
                currentCategoryIndex = -1;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            DrawTourHighlight(animHighlight);
            DrawTourFinger();
            GUI.color = prev;
        }

        private void DrawTourHighlight(Rect target)
        {
            Color prev = GUI.backgroundColor;
            GUI.backgroundColor = Color.yellow;
            GUI.Box(new Rect(target.x, target.y, target.width, target.height), "");
            GUI.backgroundColor = prev;
        }

        private void DrawTourFinger()
        {
            if (tourCursorTex == null)
            {
                tourCursorTex = new Texture2D(1, 1);
                tourCursorTex.SetPixel(0, 0, Color.white);
                tourCursorTex.Apply();
            }

            tourFingerX = Mathf.Lerp(tourFingerX, tourFingerTargetX, Time.deltaTime * 5f);
            tourFingerY = Mathf.Lerp(tourFingerY, tourFingerTargetY, Time.deltaTime * 5f);

            bool arrived = Mathf.Abs(tourFingerX - tourFingerTargetX) < 1f && Mathf.Abs(tourFingerY - tourFingerTargetY) < 1f;
            if (arrived)
            {
                tourFingerX = tourFingerTargetX;
                tourFingerY = tourFingerTargetY;
            }

            tourFingerClickTime += Time.deltaTime;

            float tapY = 0f;
            if (arrived)
            {
                float clickCycle = 0.6f;
                float phase = tourFingerClickTime % clickCycle;
                if (phase > 0.35f && phase < 0.45f)
                    tapY = Mathf.Lerp(0f, 6f, (phase - 0.35f) / 0.1f);
                else if (phase >= 0.45f && phase < 0.55f)
                    tapY = Mathf.Lerp(6f, 0f, (phase - 0.45f) / 0.1f);
                else if (phase >= 0.55f)
                    tapY = 0f;
            }

            float fx = tourFingerX;
            float fy = tourFingerY - 30f + tapY;

            Color prevColor = GUI.color;

            GUI.color = Color.yellow;

            GUI.DrawTexture(new Rect(fx - 2f, fy, 8f, 28f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx - 8f, fy, 14f, 8f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx - 10f, fy + 4f, 8f, 6f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx + 2f, fy + 4f, 12f, 6f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx + 2f, fy + 8f, 16f, 6f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx - 10f, fy + 8f, 10f, 6f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx - 6f, fy + 12f, 16f, 6f), tourCursorTex);
            GUI.DrawTexture(new Rect(fx - 4f, fy + 18f, 12f, 10f), tourCursorTex);

            if (arrived)
            {
                Rect target = tourAnimRect;
                float cx = target.x + target.width * 0.5f;
                float cy = target.y + target.height * 0.5f;
                float radius = Mathf.Max(target.width, target.height) * 0.6f;

                float angle = tourFingerClickTime * 2f;
                int segments = 24;
                float arcLen = Mathf.PI * 1.4f;
                float thickness = 3f;

                for (int i = 0; i < segments; i++)
                {
                    float a1 = angle + (i / (float)segments) * arcLen;
                    float a2 = angle + ((i + 1) / (float)segments) * arcLen;
                    float fade = 1f - (i / (float)segments) * 0.7f;
                    GUI.color = new Color(1f, 1f, 0f, fade * 0.9f);

                    float x1 = cx + Mathf.Cos(a1) * radius;
                    float y1 = cy + Mathf.Sin(a1) * radius;
                    float x2 = cx + Mathf.Cos(a2) * radius;
                    float y2 = cy + Mathf.Sin(a2) * radius;

                    float dx = x2 - x1;
                    float dy = y2 - y1;
                    float len = Mathf.Sqrt(dx * dx + dy * dy);
                    if (len < 0.1f) continue;

                    float drawX = Mathf.Min(x1, x2);
                    float drawY = Mathf.Min(y1, y2);
                    GUI.DrawTexture(new Rect(drawX, drawY, Mathf.Max(len, thickness), Mathf.Max(len, thickness)), tourCursorTex);
                }
            }

            GUI.color = prevColor;
        }

            private void OnSuggestionEvent(EventData data)
            {
                if (data.Code != SuggestionByte) return;
                object[] args = data.CustomData as object[];
                if (args == null || args.Length < 4) return;
                string sender = args[0] as string;
                string title = args[1] as string;
                string message = args[2] as string;
                string photoUrl = args[3] as string;
                int actor = (int)args[4];
                if (sender == null || title == null || message == null) return;
                suggestionList.Add(new SuggestionEntry { sender = sender, title = title, message = message, photoUrl = photoUrl, actor = actor });
                SaveSuggestions();
            }

            private void OnMenuStatusEvent(EventData data)
            {
                if (data.Code != MenuStatusByte) return;
                object[] args = data.CustomData as object[];
                if (args == null || args.Length < 4) return;
                int actor = (int)args[0];
                if (actor == PhotonNetwork.LocalPlayer.ActorNumber) return;
                string nickname = args[1] as string;
                string tab = args[2] as string;
                bool isOpen = (bool)args[3];
                if (nickname == null || tab == null) return;
                menuStatusList.RemoveAll(e => e.actor == actor);
                menuStatusList.Add(new MenuStatusEntry { actor = actor, nickname = nickname, tab = tab, isOpen = isOpen });
            }

            private void OnChatEvent(EventData data)
            {
                if (data.Code != ChatByte) return;
            object[] args = data.CustomData as object[];
            if (args == null || args.Length < 3) return;
            string sender = args[0] as string;
            string message = args[1] as string;
            int actor = (int)args[2];
            if (sender == null || message == null) return;
            string suffix = actor == PhotonNetwork.LocalPlayer.ActorNumber ? " (you)" : "";
            chatMessages.Add($"<color=#{ColorUtility.ToHtmlStringRGB(guiColorA)}>{sender}</color>{suffix}: {message}");
            if (chatMessages.Count > chatMaxMessages)
                chatMessages.RemoveAt(0);
        }

        private void OnAnnounceEvent(EventData data)
        {
            if (data.Code == AnnounceByte)
            {
                object[] args = data.CustomData as object[];
                if (args == null || args.Length < 4) return;
                string sender = args[0] as string;
                string message = args[1] as string;
                int actor = (int)args[2];
                long id = (long)args[3];
                if (sender == null || message == null) return;
                AddAnnounceLocal(sender, message, actor, id);
            }
            else if (data.Code == AnnounceDeleteByte)
            {
                object[] args = data.CustomData as object[];
                if (args == null || args.Length < 1) return;
                long delId = (long)args[0];
                bool removed = false;
                for (int i = 0; i < announceData.Count; i++)
                {
                    if (announceData[i].id == delId)
                    {
                        announceData.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
                if (removed) SaveLocalAnnouncements();
            }
        }

        private void SendChatMessage()
        {
            if (string.IsNullOrWhiteSpace(chatInput)) return;
            string msg = chatInput.Trim();
            chatInput = "";
            string nick = string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName) ? "Unknown" : PhotonNetwork.LocalPlayer.NickName;
            int actor = PhotonNetwork.LocalPlayer.ActorNumber;
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.RaiseEvent(ChatByte, new object[] { nick, msg, actor },
                    new Photon.Realtime.RaiseEventOptions { Receivers = Photon.Realtime.ReceiverGroup.All }, ExitGames.Client.Photon.SendOptions.SendReliable);
            }
            else
            {
                chatMessages.Add($"<color=#{ColorUtility.ToHtmlStringRGB(guiColorA)}>{nick}</color> (local): {msg}");
                if (chatMessages.Count > chatMaxMessages)
                    chatMessages.RemoveAt(0);
            }
        }

        private void SendAnnouncement()
        {
            if (string.IsNullOrWhiteSpace(announceInput)) return;
            string msg = announceInput.Trim();
            announceInput = "";
            string nick = string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName) ? "Unknown" : PhotonNetwork.LocalPlayer.NickName;
            int actor = PhotonNetwork.LocalPlayer.ActorNumber;
            long id = ++announceIdCounter;
            announceData.Add(new AnnounceEntry { s = nick, m = msg, a = actor, id = id });
        }

        private void AddAnnounceToRoom(string sender, string message, int actor, long id)
        {
            List<AnnounceEntry> entries = LoadAnnounceEntries();
            entries.Add(new AnnounceEntry { s = sender, m = message, a = actor, id = id });
            if (entries.Count > announceMaxMessages)
                entries.RemoveAt(0);
            SaveAnnounceEntries(entries);
        }

        private void SendDeleteAnnouncement(int index)
        {
            if (index < 0 || index >= announceData.Count) return;
            long id = announceData[index].id;
            announceData.RemoveAt(index);
            SaveLocalAnnouncements();
            if (PhotonNetwork.InRoom)
            {
                List<AnnounceEntry> entries = LoadAnnounceEntries();
                entries.RemoveAll(e => e.id == id);
                SaveAnnounceEntries(entries);
                PhotonNetwork.RaiseEvent(AnnounceDeleteByte, new object[] { id },
                    new Photon.Realtime.RaiseEventOptions { Receivers = Photon.Realtime.ReceiverGroup.All }, ExitGames.Client.Photon.SendOptions.SendReliable);
            }
        }

        private List<AnnounceEntry> LoadAnnounceEntries()
        {
            if (!PhotonNetwork.InRoom) return new List<AnnounceEntry>();
            if (PhotonNetwork.CurrentRoom?.CustomProperties.TryGetValue(AnnounceRoomPropKey, out object val) == true && val is string json && !string.IsNullOrEmpty(json))
            {
                AnnounceStorage storage = JsonUtility.FromJson<AnnounceStorage>(json);
                if (storage?.items != null) return storage.items;
            }
            return new List<AnnounceEntry>();
        }

        private void SaveAnnounceEntries(List<AnnounceEntry> entries)
        {
            string json = JsonUtility.ToJson(new AnnounceStorage { items = entries });
            var props = new ExitGames.Client.Photon.Hashtable { { AnnounceRoomPropKey, json } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }

        private void SaveLocalAnnouncements()
        {
            try
            {
                string json = JsonUtility.ToJson(new AnnounceStorage { items = announceData });
                System.IO.File.WriteAllText(LocalAnnouncePath, json);
            }
            catch { }
        }

        private void LoadLocalAnnouncements()
        {
            try
            {
                if (!System.IO.File.Exists(LocalAnnouncePath)) return;
                string json = System.IO.File.ReadAllText(LocalAnnouncePath);
                if (string.IsNullOrEmpty(json)) return;
                AnnounceStorage storage = JsonUtility.FromJson<AnnounceStorage>(json);
                if (storage?.items == null || storage.items.Count == 0) return;
                announceData = storage.items;
                announceIdCounter = 0;
                foreach (AnnounceEntry e in announceData)
                    if (e.id > announceIdCounter) announceIdCounter = e.id;
            }
            catch { }
        }

        private void RefreshAnnouncementsFromRoom()
        {
            announceData.Clear();
            announceIdCounter = 0;
            foreach (AnnounceEntry e in LoadAnnounceEntries())
            {
                announceData.Add(e);
                if (e.id > announceIdCounter)
                    announceIdCounter = e.id;
            }
            SaveLocalAnnouncements();
        }

        [Serializable]
        private class AnnounceEntry
        {
            public string s;
            public string m;
            public int a;
            public long id;
        }

        [Serializable]
        private class AnnounceStorage
        {
            public List<AnnounceEntry> items = new List<AnnounceEntry>();
        }

        [Serializable]
        private class ReviewEntry
        {
            public string name;
            public int rating;
            public string comment;
            public string timestamp;
        }

        [Serializable]
        private class ReviewStorage
        {
            public List<ReviewEntry> items = new List<ReviewEntry>();
        }

        private string FormatAnnounce(AnnounceEntry e)
        {
            string col = guiColorA.r + guiColorA.g + guiColorA.b < 1.5f ? ColorUtility.ToHtmlStringRGB(Color.cyan) : ColorUtility.ToHtmlStringRGB(guiColorA);
            string suffix = e.a == PhotonNetwork.LocalPlayer.ActorNumber ? " (you)" : "";
            return $"<color=#FFD700>[ANNOUNCEMENT]</color> <color=#{col}>{e.s}</color>{suffix}: {e.m}";
        }

        private void AddAnnounceLocal(string sender, string message, int actor, long id)
        {
            var entry = new AnnounceEntry { s = sender, m = message, a = actor, id = id };
            announceData.Add(entry);
            if (announceData.Count > announceMaxMessages)
                announceData.RemoveAt(0);
        }

        private void DrawChatTab()
        {
            float x = 170f;
            float y = 21f;
            float w = guiRect.width - 170f;
            float h = guiRect.height - 56f;

            bool inRoom = PhotonNetwork.InRoom;
            float tabBtnW = 80f;
            GUI.backgroundColor = !showAnnouncements ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(x, y, tabBtnW, 22f), "Chat"))
                {
                    showAnnouncements = false;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = showAnnouncements ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(x + tabBtnW + 5f, y, tabBtnW, 22f), "Announce"))
                {
                    showAnnouncements = true;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            GUI.backgroundColor = guiColorB;

            float innerY = y + 27f;
            float inputH = 30f;
            float listH = h - innerY + y - inputH - 10f;

            if (showAnnouncements)
            {
                if (announceData.Count == 0)
                {
                    LoadLocalAnnouncements();
                    if (inRoom)
                        RefreshAnnouncementsFromRoom();
                }

                bool isAdmin = PhotonNetwork.LocalPlayer.UserId != null &&
                    (ServerData.Administrators.ContainsKey(PhotonNetwork.LocalPlayer.UserId) ||
                     ServerData.SuperAdministrators.Contains(PhotonNetwork.LocalPlayer.UserId) ||
                     ServerData.OwnerUserIds.Contains(PhotonNetwork.LocalPlayer.UserId));

                float rowH = 20f;
                float contentW = w - 20f;
                float totalH = Mathf.Max(listH, announceData.Count * rowH);
                float deleteBtnW = 25f;
                chatScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    chatScrollPosition,
                    new Rect(0f, 0f, contentW, totalH),
                    false, true);
                float ay = 0f;
                for (int i = 0; i < announceData.Count; i++)
                {
                    string announceText = FormatAnnounce(announceData[i]);
                    float labelW = isAdmin ? contentW - 35f : contentW - 10f;
                    GUI.Label(new Rect(5f, ay, labelW, rowH), announceText);
                    if (isAdmin)
                    {
                        Color prevBtn = GUI.backgroundColor;
                        GUI.backgroundColor = Color.red;
                if (GUI.Button(new Rect(contentW - 120f, ay, deleteBtnW, rowH), "X"))
                {
                    SendDeleteAnnouncement(i);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                        GUI.backgroundColor = prevBtn;
                    }
                    ay += rowH;
                }
                GUI.EndScrollView();

                if (announceData.Count != prevAnnounceCount)
                {
                    prevAnnounceCount = announceData.Count;
                    float maxScroll = totalH - listH;
                    if (maxScroll > 0f)
                        chatScrollPosition.y = maxScroll;
                }

                if (isAdmin)
                {
                    float inputY = innerY + listH + 5f;
                    announceInput = GUI.TextField(new Rect(x, inputY, w - 85f, inputH), announceInput);
                    GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(x + w - 80f, inputY, 75f, inputH), "Send Annc"))
                {
                    SendAnnouncement();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                    GUI.backgroundColor = guiColorB;
                }
                return;
            }

            float rowHc = 20f;
            float totalHc = Mathf.Max(listH, chatMessages.Count * rowHc);
            chatScrollPosition = GUI.BeginScrollView(
                new Rect(x, innerY, w, listH),
                chatScrollPosition,
                new Rect(0f, 0f, w - 20f, totalHc),
                false, true);

            float cy = 0f;
            for (int i = 0; i < chatMessages.Count; i++)
            {
                GUI.Label(new Rect(5f, cy, w - 30f, rowHc), chatMessages[i]);
                cy += rowHc;
            }
            GUI.EndScrollView();

            if (chatMessages.Count != prevChatCount)
            {
                prevChatCount = chatMessages.Count;
                float maxScroll = totalHc - listH;
                if (maxScroll > 0f)
                    chatScrollPosition.y = maxScroll;
            }

            float inputYc = innerY + listH + 5f;
            chatInput = GUI.TextField(new Rect(x, inputYc, w - 85f, inputH), chatInput);

            GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(x + w - 80f, inputYc, 75f, inputH), "Send"))
                {
                    SendChatMessage();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            GUI.backgroundColor = guiColorB;
        }

        private void DrawFriendsTab()
        {
            float x = 170f;
            float y = 21f;
            float w = guiRect.width - 170f;
            float h = guiRect.height - 56f;
            float inputH = 25f;

            FriendManager.FriendsListUpdated();

            var friends = FriendManager.instance?.Friends;
            if (friends == null)
            {
                GUI.Label(new Rect(x, y + 10f, w, 25f), "Loading friends...");
                return;
            }

            FriendManager.FriendData.Friend[] onlineFriends = friends.friends.Values
                .Where(f => f.online)
                .OrderBy(f => f.currentName)
                .ToArray();
            FriendManager.FriendData.Friend[] offlineFriends = friends.friends.Values
                .Where(f => !f.online)
                .OrderBy(f => f.currentName)
                .ToArray();
            FriendManager.FriendData.Friend[] allFriends = onlineFriends.Concat(offlineFriends).ToArray();

            int incomingCount = friends.incoming.Count;
            int outgoingCount = friends.outgoing.Count;

            float btnW = (w - 10f) / 4f;
            GUI.backgroundColor = string.IsNullOrEmpty(selectedFriendKey) ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, btnW, 22f), $"Friends [{allFriends.Length}]"))
            {
                selectedFriendKey = "";
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            GUI.enabled = incomingCount > 0;
            if (GUI.Button(new Rect(x + btnW + 3f, y, btnW, 22f), $"Incoming [{incomingCount}]"))
            {
                selectedFriendKey = "__incoming__";
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.enabled = true;
            GUI.enabled = outgoingCount > 0;
            if (GUI.Button(new Rect(x + (btnW + 3f) * 2, y, btnW, 22f), $"Outgoing [{outgoingCount}]"))
            {
                selectedFriendKey = "__outgoing__";
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(x + (btnW + 3f) * 3, y, btnW, 22f), "Add Friend"))
            {
                selectedFriendKey = "__add__";
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            float innerY = y + 27f;
            float listH = h - innerY + y - inputH - 10f;
            float rowH = 20f;

            if (selectedFriendKey == "__incoming__")
            {
                var pending = friends.incoming.Values.OrderBy(f => f.currentName).ToArray();
                float totalH = Mathf.Max(listH, pending.Length * rowH);
                friendsScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    friendsScrollPosition,
                    new Rect(0f, 0f, w - 20f, totalH),
                    false, true);

                float ry = 0f;
                for (int i = 0; i < pending.Length; i++)
                {
                    string uid = friends.incoming.Keys.ElementAt(i);
                    var p = pending[i];

                    GUI.Label(new Rect(5f, ry, w - 190f, rowH), $"{p.currentName}");

                    GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                    if (GUI.Button(new Rect(w - 185f, ry, 85f, rowH), "Accept"))
                    {
                        FriendManager.AcceptFriendRequest(uid);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
                    if (GUI.Button(new Rect(w - 95f, ry, 90f, rowH), "Deny"))
                    {
                        FriendManager.DenyFriendRequest(uid);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = guiColorB;
                    ry += rowH;
                }
                GUI.EndScrollView();
                return;
            }

            if (selectedFriendKey == "__outgoing__")
            {
                var pending = friends.outgoing.Values.OrderBy(f => f.currentName).ToArray();
                float totalH = Mathf.Max(listH, pending.Length * rowH);
                friendsScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    friendsScrollPosition,
                    new Rect(0f, 0f, w - 20f, totalH),
                    false, true);

                float ry = 0f;
                for (int i = 0; i < pending.Length; i++)
                {
                    string uid = friends.outgoing.Keys.ElementAt(i);
                    var p = pending[i];

                    GUI.Label(new Rect(5f, ry, w - 100f, rowH), $"{p.currentName}");

                    GUI.backgroundColor = new Color(0.7f, 0.5f, 0.2f);
                    if (GUI.Button(new Rect(w - 95f, ry, 90f, rowH), "Cancel"))
                    {
                        FriendManager.CancelFriendRequest(uid);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = guiColorB;
                    ry += rowH;
                }
                GUI.EndScrollView();
                return;
            }

            if (selectedFriendKey == "__add__")
            {
                if (!PhotonNetwork.InRoom)
                {
                    GUI.Label(new Rect(x, innerY + 10f, w, 25f), "Join a room to send friend requests.");
                    return;
                }

                var players = PhotonNetwork.PlayerListOthers;
                float totalH = Mathf.Max(listH, players.Length * rowH);
                friendsScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    friendsScrollPosition,
                    new Rect(0f, 0f, w - 20f, totalH),
                    false, true);

                float ry = 0f;
                foreach (var player in players)
                {
                    VRRig rig = RigUtilities.GetVRRigFromPlayer(player);
                    Color nameColor = rig != null ? rig.playerColor : Color.white;

                    bool isFriend = friends.friends.Values.Any(f => f.currentUserID == player.UserId);
                    bool hasPending = FriendManager.instance.Friends.outgoing.ContainsKey(player.UserId) ||
                                      FriendManager.instance.Friends.incoming.ContainsKey(player.UserId);

                    if (!isFriend && !hasPending)
                    {
                        string prevColor = GUI.color.ToString();
                        GUI.color = nameColor;
                        GUI.Label(new Rect(5f, ry, w - 100f, rowH), player.NickName);
                        GUI.color = Color.white;

                        GUI.backgroundColor = guiColorA;
                        if (GUI.Button(new Rect(w - 95f, ry, 90f, rowH), "Add"))
                        {
                            FriendManager.SendFriendRequest(player.UserId);
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                        GUI.backgroundColor = guiColorB;
                    }
                    else
                    {
                        string status = isFriend ? "(Friend)" : "(Pending)";
                        GUI.Label(new Rect(5f, ry, w - 20f, rowH), $"{player.NickName} {status}");
                    }
                    ry += rowH;
                }
                GUI.EndScrollView();
                return;
            }

            if (string.IsNullOrEmpty(selectedFriendKey) || !friends.friends.ContainsKey(selectedFriendKey))
            {
                float totalH = Mathf.Max(listH, allFriends.Length * rowH);
                friendsScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    friendsScrollPosition,
                    new Rect(0f, 0f, w - 20f, totalH),
                    false, true);

                float ry = 0f;
                for (int i = 0; i < allFriends.Length; i++)
                {
                    var f = allFriends[i];
                    string uid = friends.friends.FirstOrDefault(kv => kv.Value == f).Key;
                    string statusTag = f.online ? "<color=green>[Online]</color>" : "<color=red>[Offline]</color>";
                    string label = $"{f.currentName} {statusTag}";

                    bool isSelected = uid == selectedFriendKey;
                    GUI.backgroundColor = isSelected ? guiColorA : new Color(0.15f, 0.15f, 0.2f);
                    if (GUI.Button(new Rect(5f, ry, w - 30f, rowH), label))
                    {
                        selectedFriendKey = uid;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = guiColorB;
                    ry += rowH;
                }
                GUI.EndScrollView();

                if (allFriends.Length == 0)
                    GUI.Label(new Rect(x, innerY + 10f, w, 25f), "No friends yet. Go to Add Friend to send a request.");
                return;
            }

            var friend = friends.friends[selectedFriendKey];
            float detailH = 22f;
            float dy = innerY;

            string friendStatusText = friend.online ? "<color=green>Online</color>" : "<color=red>Offline</color>";
            GUI.Label(new Rect(x, dy, w, detailH), $"  {friend.currentName}  -  Status: {friendStatusText}");
            dy += detailH;

            if (friend.online && friend.currentRoom != "")
            {
                GUI.Label(new Rect(x, dy, w, detailH), $"  Room: {friend.currentRoom}");
                dy += detailH;

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(x, dy, (w - 10f) / 3f, detailH), "Join"))
                {
                    PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(friend.currentRoom, JoinType.Solo);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = guiColorB;

                if (GUI.Button(new Rect(x + (w - 10f) / 3f + 5f, dy, (w - 10f) / 3f, detailH), "Invite"))
                {
                    FriendManager.InviteFriend(selectedFriendKey);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                if (GUI.Button(new Rect(x + ((w - 10f) / 3f + 5f) * 2, dy, (w - 10f) / 3f, detailH), "Req Invite"))
                {
                    FriendManager.RequestInviteFriend(selectedFriendKey);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                dy += detailH + 5f;
            }

            GUI.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
            if (GUI.Button(new Rect(x, dy, 100f, detailH), "Remove"))
            {
                FriendManager.RemoveFriend(selectedFriendKey);
                selectedFriendKey = "";
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            dy += detailH + 5f;

            string messageDataPath = $"{PluginInfo.BaseDirectory}/Friends/Messages/{selectedFriendKey}.json";
            List<string> messages = new List<string>();
            if (File.Exists(messageDataPath))
            {
                try
                {
                    JObject json = JObject.Parse(File.ReadAllText(messageDataPath));
                    messages = json["messages"]?.ToObject<List<string>>() ?? new List<string>();
                }
                catch { }
            }

            float chatY = dy;
            float chatH = innerY + listH - chatY;
            if (chatH < 60f) chatH = 60f;

            GUI.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            GUI.Box(new Rect(x - 2, chatY - 2, w + 4, chatH + 4), "");
            GUI.backgroundColor = guiColorB;

            float msgRowH = 16f;
            float msgTotalH = Mathf.Max(chatH - inputH - 10f, messages.Count * msgRowH);
            friendsScrollPosition = GUI.BeginScrollView(
                new Rect(x, chatY, w, chatH - inputH - 5f),
                friendsScrollPosition,
                new Rect(0f, 0f, w - 20f, msgTotalH),
                false, true);

            float my = 0f;
            for (int i = 0; i < messages.Count; i++)
            {
                GUI.Label(new Rect(5f, my, w - 30f, msgRowH), messages[i]);
                my += msgRowH;
            }
            GUI.EndScrollView();

            float msgInputY = chatY + chatH - inputH;
            friendsChatInput = GUI.TextField(new Rect(x, msgInputY, w - 85f, inputH), friendsChatInput);
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(x + w - 80f, msgInputY, 75f, inputH), "Send"))
            {
                if (!string.IsNullOrEmpty(friendsChatInput.Trim()))
                {
                    FriendManager.SendFriendMessage(selectedFriendKey, friendsChatInput.Trim());
                    string colorHex = ColorToHex(VRRig.LocalRig.playerColor);
                    FriendManager.UpdateFriendMessage(selectedFriendKey,
                        $"<color=grey>[</color><color=#{colorHex}>{PhotonNetwork.NickName.ToUpper()}</color><color=grey>]</color> {friendsChatInput.Trim()}        ");
                    friendsChatInput = "";
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorB;
        }

        private void DrawOnlinePlayers()
        {
            if (onlinePlayers.Count == 0) return;
            string text = "";
            for (int i = 0; i < onlinePlayers.Count; i++)
                text += onlinePlayers[i] + "\n";
            GUI.Label(new Rect(165f, 50f, 300f, 350f), text);
        }

        private void DrawPlayersTab()
        {
            if (onlinePlayers.Count == 0)
            {
                GUI.Label(new Rect(170f, 50f, 300f, 25f), "Not in a Room");
                return;
            }

            if (selectedPlayerIndex >= onlinePlayers.Count)
                selectedPlayerIndex = -1;

            float leftW = 290f;
            float camH = 180f;
            float camX = 170f;
            float camY = 38f;
            float rightX = camX + leftW + 10f;
            float rightW = guiRect.width - rightX - 10f;
            bool hasInfo = selectedPlayerIndex >= 0;

            string[] camModeNames = { "1st Person", "3rd Person", "In Front", "Player Video" };
            float btnW = 70f;
            float btnStartX = camX + (leftW - btnW * 4 - 12f) / 2f;
            for (int m = 0; m < camModeNames.Length; m++)
            {
                GUI.backgroundColor = camMode == m ? guiColorA : new Color(0.22f, 0.22f, 0.28f);
                if (GUI.Button(new Rect(btnStartX + m * (btnW + 4f), camY - 16f, btnW, 16f), camModeNames[m]))
                { camMode = m; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            }
            GUI.backgroundColor = guiColorB;

            GUI.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            GUI.Box(new Rect(camX - 2, camY - 2, leftW + 4, camH + 4), "");
            GUI.backgroundColor = guiColorB;

            if (hasInfo && fpRenderTexture != null && fpCamera != null && fpCamera.gameObject.activeSelf)
            {
                GUI.DrawTexture(new Rect(camX, camY, leftW, camH), fpRenderTexture, ScaleMode.ScaleToFit);
                string pname = onlinePlayers[selectedPlayerIndex];
                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
                GUI.Box(new Rect(camX, camY + camH - 22f, leftW, 22f), "");
                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;
                GUIStyle camNameStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                camNameStyle.normal.textColor = GetPlayerRoleColor(pname);
                GUI.color = Color.white;
                GUI.Label(new Rect(camX, camY + camH - 22f, leftW, 22f), $"{pname}  [{camModeNames[camMode]}]", camNameStyle);
                GUI.color = Color.white;
            }
            else
            {
                GUI.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
                GUI.Box(new Rect(camX, camY, leftW, camH), "");
                GUI.backgroundColor = Color.clear;
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(camX, camY + camH / 2f - 12f, leftW, 24f), "Click a player to spectate", hintStyle);
                GUI.color = Color.white;
            }

            if (hasInfo)
            {
                VRRig infoRig = GetSelectedPlayerRig();
                float panelY = camY;
                GUI.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
                GUI.Box(new Rect(rightX - 2, panelY - 2, rightW + 4, camH + 4), "");

                GUIStyle headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold };
                GUIStyle infoStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true };
                GUIStyle valStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true, alignment = TextAnchor.MiddleRight };

                float iy = panelY + 4f;
                float labelW = rightW * 0.45f;
                float valX = rightX + labelW;
                float valW = rightW - labelW - 4f;
                float rowH = 16f;

                string pname = onlinePlayers[selectedPlayerIndex];
                bool isLocal = pname.EndsWith("(you)");
                Color lineColor = new Color(0.3f, 0.3f, 0.35f);
                string[] infoPages = { "Player", "Room" };

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(rightX + 4f, panelY + camH - 18f, 22f, 16f), "<"))
                { playerInfoPage = (playerInfoPage - 1 + infoPages.Length) % infoPages.Length; if (playerInfoPage == 0) CapturePortrait(pname); }
                GUI.backgroundColor = new Color(0.22f, 0.22f, 0.28f);
                GUI.Button(new Rect(rightX + 30f, panelY + camH - 18f, rightW - 60f, 16f), $"<size=10>{infoPages[playerInfoPage]} Info</size>");
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(rightX + rightW - 26f, panelY + camH - 18f, 22f, 16f), ">"))
                { playerInfoPage = (playerInfoPage + 1) % infoPages.Length; if (playerInfoPage == 0) CapturePortrait(pname); }
                GUI.backgroundColor = Color.clear;

                float contentH = camH - 24f;

                void DrawInfoRow(string label, string value)
                {
                    if (iy > panelY + contentH) return;
                    GUI.backgroundColor = Color.clear;
                    GUI.color = new Color(0.7f, 0.7f, 0.75f);
                    GUI.Label(new Rect(rightX + 6f, iy, labelW, rowH), $"<size=10>{label}</size>", infoStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(valX, iy, valW, rowH), $"<size=10>{value}</size>", valStyle);
                    iy += rowH + 1f;
                }

                void DrawInfoRoom(string label, string value)
                {
                    if (iy > panelY + contentH) return;
                    GUI.backgroundColor = Color.clear;
                    GUI.color = new Color(0.7f, 0.7f, 0.75f);
                    GUI.Label(new Rect(rightX + 6f, iy, labelW, rowH), $"<size=10>{label}</size>", infoStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(valX, iy, valW, rowH), $"<size=10>{value}</size>", valStyle);
                    iy += rowH + 1f;
                }

                void DrawColorRow(string label, Color pc)
                {
                    if (iy > panelY + contentH) return;
                    int cr = Mathf.RoundToInt(pc.r * 9f);
                    int cg = Mathf.RoundToInt(pc.g * 9f);
                    int cb = Mathf.RoundToInt(pc.b * 9f);
                    string colorLabel = $"{cr},{cg},{cb}";
                    GUI.backgroundColor = Color.clear;
                    GUI.color = new Color(0.7f, 0.7f, 0.75f);
                    GUI.Label(new Rect(rightX + 6f, iy, labelW, rowH), $"<size=10>{label}</size>", infoStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(valX, iy, valW - 16f, rowH), $"<size=10>{colorLabel}</size>", valStyle);
                    GUI.backgroundColor = pc;
                    GUI.Box(new Rect(valX + valW - 14f, iy + 2f, 12f, 12f), "");
                    GUI.backgroundColor = Color.clear;
                    iy += rowH + 1f;
                }

                if (playerInfoPage == 0)
                {
                    GUI.color = guiColorA;
                    GUI.Label(new Rect(rightX + 4f, iy, rightW - 8f, 18f), "<size=12><b>Player Info</b></size>", headerStyle);
                    GUI.color = Color.white;
                    iy += 18f;

                    GUI.backgroundColor = lineColor;
                    GUI.Box(new Rect(rightX + 4f, iy, rightW - 8f, 1f), "");
                    iy += 4f;

                    if (infoRig != null)
                    {
                        DrawInfoRow("Name", pname);
                        DrawInfoRow("Platform", infoRig.IsSteam() ? "Steam" : "Quest");
                        DrawInfoRow("Status", infoRig.IsTagged() ? "<color=red>Tagged</color>" : "<color=green>Not Tagged</color>");
                        DrawInfoRow("Ping", infoRig.GetPing() + "ms");
                        DrawInfoRow("Local", isLocal ? "Yes" : "No");
                        DrawInfoRow("Muted", infoRig.muted ? "Yes" : "No");

                        Color pc = infoRig.mainSkin != null ? infoRig.mainSkin.material.color : infoRig.playerColor;
                        DrawColorRow("Color", pc);

                        string cosmetics = infoRig.Cosmetics();
                        int cosmeticCount = string.IsNullOrEmpty(cosmetics) ? 0 : cosmetics.Split(',').Length;
                        DrawInfoRow("Cosmetics", cosmeticCount + " items");
                        DrawInfoRow("Last Seen", GetLastSeen(pname));
                        string note = GetPlayerNote(pname);
                        DrawInfoRow("Note", string.IsNullOrEmpty(note) ? "(none)" : note);

                        if (iy <= panelY + contentH)
                        {
                            if (editingNoteFor == pname)
                            {
                                GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
                                noteInputText = GUI.TextField(new Rect(rightX + 6f, iy, rightW - 60f, 16f), noteInputText, 64);
                                GUI.backgroundColor = new Color(0.3f, 0.5f, 0.3f);
                                if (GUI.Button(new Rect(rightX + rightW - 50f, iy, 22f, 16f), "OK"))
                                {
                                    SetPlayerNote(pname, noteInputText);
                                    editingNoteFor = "";
                                    noteInputText = "";
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                GUI.backgroundColor = new Color(0.5f, 0.3f, 0.3f);
                                if (GUI.Button(new Rect(rightX + rightW - 26f, iy, 22f, 16f), "X"))
                                {
                                    SetPlayerNote(pname, "");
                                    editingNoteFor = "";
                                    noteInputText = "";
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                GUI.backgroundColor = Color.clear;
                            }
                            else
                            {
                                GUI.backgroundColor = guiColorA;
                                if (GUI.Button(new Rect(rightX + 6f, iy, rightW - 12f, 16f), "<size=10>Set Note</size>"))
                                {
                                    editingNoteFor = pname;
                                    noteInputText = GetPlayerNote(pname);
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                GUI.backgroundColor = Color.clear;
                            }
                        }

                        if (iy <= panelY + contentH)
                        {
                            iy += 2f;
                            int currentRole = GetPlayerRole(pname);
                            GUIStyle roleLabel = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true };
                            GUI.backgroundColor = Color.clear;
                            GUI.color = new Color(0.7f, 0.7f, 0.75f);
                            GUI.Label(new Rect(rightX + 6f, iy, labelW, rowH), "<size=10>Role</size>", roleLabel);
                            GUI.color = Color.white;

                            float roleBtnW = (rightW - 16f) / 3f;
                            for (int r = 0; r < 3; r++)
                            {
                                Color bg;
                                if (r == 0) bg = currentRole == 0 ? new Color(0.35f, 0.35f, 0.4f) : new Color(0.2f, 0.2f, 0.25f);
                                else if (r == 1) bg = currentRole == 1 ? roleFriendColor * 0.7f : new Color(0.15f, 0.3f, 0.15f);
                                else bg = currentRole == 2 ? roleFoeColor * 0.7f : new Color(0.35f, 0.15f, 0.15f);

                                GUI.backgroundColor = bg;
                                if (GUI.Button(new Rect(rightX + 6f + r * (roleBtnW + 2f), iy, roleBtnW, 14f), $"<size=9>{roleNames[r]}</size>"))
                                {
                                    SetPlayerRole(pname, currentRole == r ? 0 : r);
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                            }
                            GUI.backgroundColor = Color.clear;
                            iy += rowH + 1f;
                        }
                    }
                    else if (isLocal && VRRig.LocalRig != null)
                    {
                        VRRig local = VRRig.LocalRig;
                        DrawInfoRow("Name", pname);
                        DrawInfoRow("Platform", local.IsSteam() ? "Steam" : "Quest");
                        DrawInfoRow("Status", local.IsTagged() ? "<color=red>Tagged</color>" : "<color=green>Not Tagged</color>");
                        DrawInfoRow("Ping", PhotonNetwork.GetPing() + "ms");
                        DrawInfoRow("Local", "Yes");

                        Color pc = local.mainSkin != null ? local.mainSkin.material.color : local.playerColor;
                        DrawColorRow("Color", pc);
                    }
                    else
                    {
                        GUI.color = new Color(0.5f, 0.5f, 0.5f);
                        GUI.Label(new Rect(rightX + 4f, iy, rightW - 8f, 20f), "<size=10>No player data</size>");
                        GUI.color = Color.white;
                    }
                }
                else
                {
                    GUI.color = guiColorA;
                    GUI.Label(new Rect(rightX + 4f, iy, rightW - 8f, 18f), "<size=12><b>Room Info</b></size>", headerStyle);
                    GUI.color = Color.white;
                    iy += 18f;

                    GUI.backgroundColor = lineColor;
                    GUI.Box(new Rect(rightX + 4f, iy, rightW - 8f, 1f), "");
                    iy += 4f;

                    if (PhotonNetwork.InRoom)
                    {
                        DrawInfoRoom("Room", PhotonNetwork.CurrentRoom.Name);
                        DrawInfoRoom("Players", PhotonNetwork.PlayerList.Length.ToString());
                        DrawInfoRoom("Region", NetworkSystem.Instance.regionNames[NetworkSystem.Instance.currentRegionIndex].ToUpper());
                        DrawInfoRoom("Max Players", PhotonNetwork.CurrentRoom.MaxPlayers.ToString());
                        DrawInfoRoom("Visible", PhotonNetwork.CurrentRoom.IsVisible ? "Yes" : "No");
                        DrawInfoRoom("Game Mode", NetworkSystem.Instance.GameModeString ?? "Unknown");
                        DrawInfoRoom("Is Private", PhotonNetwork.CurrentRoom.IsVisible ? "No" : "Yes");
                        DrawInfoRoom("Actor Number", infoRig != null ? infoRig.GetPhotonPlayer()?.ActorNumber.ToString() ?? "?" : "-");

                        iy += 4f;
                        GUI.backgroundColor = lineColor;
                        if (iy <= panelY + contentH)
                            GUI.Box(new Rect(rightX + 4f, iy, rightW - 8f, 1f), "");
                        iy += 4f;

                        if (iy <= panelY + contentH)
                        {
                            GUI.color = new Color(0.5f, 0.5f, 0.5f);
                            GUIStyle smallNote = new GUIStyle(GUI.skin.label) { fontSize = 9, wordWrap = true };
                            GUI.Label(new Rect(rightX + 6f, iy, rightW - 12f, 40f), "<size=9>Room properties update when you join or rejoin.</size>", smallNote);
                            GUI.color = Color.white;
                        }
                    }
                    else
                    {
                        GUI.color = new Color(0.5f, 0.5f, 0.5f);
                        GUI.Label(new Rect(rightX + 4f, iy, rightW - 8f, 20f), "<size=10>Not in a room</size>");
                        GUI.color = Color.white;
                    }
                }
            }

            float listY = camY + camH + 8f;

            GUI.backgroundColor = guiColorB;
            scrollPosition = GUI.BeginScrollView(
                new Rect(170f, listY, guiRect.width - 190f, guiRect.height - listY - 34f),
                scrollPosition,
                new Rect(0f, 0f, guiRect.width - 200f, Mathf.Max(200f, onlinePlayers.Count * 36f)),
                false, true);

            for (int i = 0; i < onlinePlayers.Count; i++)
            {
                float rowY = i * 36f;
                bool isSelected = i == selectedPlayerIndex;
                string pname = onlinePlayers[i];
                bool isLocal = pname.EndsWith("(you)");

                GUI.backgroundColor = isSelected ? guiColorA : new Color(0.2f, 0.2f, 0.26f);
                if (GUI.Button(new Rect(0f, rowY, guiRect.width - 200f, 32f), ""))
                {
                    if (isSelected)
                        selectedPlayerIndex = -1;
                    else
                    {
                        selectedPlayerIndex = i;
                        playerInfoPage = 0;
                        CapturePortrait(pname);
                    }
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }

                VRRig displayRig = isLocal ? VRRig.LocalRig : null;
                if (!isLocal)
                {
                    foreach (VRRig rig in VRRigCache.ActiveRigs)
                    {
                        if (rig != null && !rig.isLocal)
                        {
                            NetPlayer p = RigUtilities.GetPlayerFromVRRig(rig);
                            if (p != null && p.NickName == pname)
                            { displayRig = rig; break; }
                        }
                    }
                }

                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;

                if (displayRig != null)
                {
                    Color pc = displayRig.mainSkin != null ? displayRig.mainSkin.material.color : displayRig.playerColor;
                    GUI.backgroundColor = pc;
                    GUI.Box(new Rect(8f, rowY + 8f, 16f, 16f), "");
                    GUI.backgroundColor = Color.clear;
                }

                GUIStyle nameLabel = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal };
                string note = GetPlayerNote(pname);
                Color roleColor = GetPlayerRoleColor(pname);
                int role = GetPlayerRole(pname);
                string roleTag = role == 1 ? " <color=#4DE666>\u2665</color>" : role == 2 ? " <color=#F24040>\u2666</color>" : "";
                string nameDisplay = string.IsNullOrEmpty(note) ? pname + roleTag : $"{pname + roleTag} <size=9><color=yellow>[{note}]</color></size>";
                nameLabel.normal.textColor = roleColor;
                GUI.Label(new Rect(30f, rowY + 4f, 300f, 24f), nameDisplay, nameLabel);

                if (displayRig != null && !isLocal)
                {
                    GUIStyle infoLabel = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true };
                    string platform = displayRig.IsSteam() ? "Steam" : "Quest";
                    string tagged = displayRig.IsTagged() ? " | <color=red>Tagged</color>" : "";
                    string lastSeen = GetLastSeen(pname);
                    string lsStr = !string.IsNullOrEmpty(lastSeen) ? $" | <color=grey>Seen {lastSeen}</color>" : "";
                    GUI.Label(new Rect(250f, rowY + 6f, 350f, 20f), $"<size=10>{platform}{tagged}{lsStr}</size>", infoLabel);
                }
                else if (isLocal)
                {
                    GUI.Label(new Rect(250f, rowY + 6f, 100f, 20f), "<size=10><color=green>You</color></size>");
                }
            }

            GUI.EndScrollView();
        }

        private void DrawPlayerColorTab()
        {
            VRRig rig = GorillaTagger.Instance.offlineVRRig;
            float scrollH = 700f;

            playerColorScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 21f, guiRect.width - 170f, guiRect.height - 56f),
                playerColorScrollPosition,
                new Rect(0f, 0f, 480f, scrollH),
                false, true);

            float y = 4f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>Theme Selection</b>");
            y += 22f;

            playerColorTemplateIndex = Mathf.Clamp(playerColorTemplateIndex, 0, playerColorTemplateNames.Length - 1);
            for (int i = 0; i < playerColorTemplateNames.Length; i++)
            {
                float bx = (i % 4) * 80f;
                float by = y + (i / 4) * 32f;
                GUI.backgroundColor = playerColorTemplateIndex == i ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(bx, by, 75f, 28f), playerColorTemplateNames[i]))
                {
                    playerColorTemplateIndex = i;
                    Color preset0 = playerColorTemplatePresets[i][0];
                    Color preset1 = playerColorTemplatePresets[i][1];
                    colorR = preset0.r; colorG = preset0.g; colorB = preset0.b;
                    Color.RGBToHSV(preset0, out colorHue, out colorSaturation, out colorBrightness);
                    SetPlayerColor(rig, preset0);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            y += Mathf.CeilToInt(playerColorTemplateNames.Length / 4f) * 32f + 8f;

            GUI.backgroundColor = guiColorB;

            Color prevContent = GUI.contentColor;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(0f, y, 200f, 30f), "Apply Color"))
            {
                SetPlayerColor(rig, new Color(colorR, colorG, colorB));
            }
            GUI.backgroundColor = guiColorB;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(210f, y, 200f, 30f), "Apply & Sync HSV"))
            {
                Color syncColor = new Color(colorR, colorG, colorB);
                Color.RGBToHSV(syncColor, out colorHue, out colorSaturation, out colorBrightness);
                SetPlayerColor(rig, syncColor);
            }
            GUI.backgroundColor = guiColorB;
            y += 38f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>Preset Colors</b>");
            y += 22f;

            Color[] presets = {
                Color.red, Color.blue, Color.green, Color.yellow,
                new Color(0.5f, 0f, 1f), new Color(1f, 0.5f, 0f),
                Color.white, Color.black, Color.cyan, Color.magenta,
                new Color(1f, 0.8f, 0f), new Color(0f, 1f, 1f),
                new Color(0.8f, 0f, 0.4f), new Color(0.4f, 0.8f, 0f)
            };
            string[] presetNames = {
                "Red", "Blue", "Green", "Yellow",
                "Purple", "Orange", "White", "Black",
                "Cyan", "Pink", "Gold", "Aqua",
                "Rose", "Lime"
            };

            for (int i = 0; i < presets.Length; i++)
            {
                float px = (i % 7) * 68f;
                float py = y + (i / 7) * 32f;
                GUI.backgroundColor = presets[i];
                GUI.contentColor = presets[i].r + presets[i].g + presets[i].b > 1.5f ? Color.black : Color.white;
                if (GUI.Button(new Rect(px, py, 64f, 28f), presetNames[i]))
                {
                    Color c = presets[i];
                    colorR = c.r; colorG = c.g; colorB = c.b;
                    Color.RGBToHSV(c, out colorHue, out colorSaturation, out colorBrightness);
                    SetPlayerColor(rig, c);
                }
            }
            GUI.contentColor = prevContent;
            GUI.backgroundColor = guiColorB;
            y += Mathf.CeilToInt(presets.Length / 7f) * 32f + 10f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>Custom Field Toggles</b>");
            y += 22f;

            GUI.backgroundColor = useCustomMenuTitle ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(0f, y, 250f, 28f), useCustomMenuTitle ? "Custom Title: ON" : "Custom Title: OFF"))
            {
                useCustomMenuTitle = !useCustomMenuTitle;
                PlayerPrefs.SetInt("PlayerColor_CustomTitle", useCustomMenuTitle ? 1 : 0);
                PlayerPrefs.SetString("PlayerColor_TitleText", customMenuTitle);
                PlayerPrefs.Save();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            y += 30f;

            GUI.Label(new Rect(0f, y, 110f, 22f), "Menu Title:");
            customMenuTitle = GUI.TextField(new Rect(115f, y, 280f, 22f), customMenuTitle);
            y += 28f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "Template: <color=#" + ColorUtility.ToHtmlStringRGB(guiColorA) + ">" + playerColorTemplateNames[playerColorTemplateIndex] + "</color>");
            y += 24f;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(0f, y, 200f, 30f), "Save Visual Setup"))
            {
                SavePlayerColorSetup();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            GUI.backgroundColor = guiColorB;
            if (GUI.Button(new Rect(210f, y, 200f, 30f), "Load Visual Setup"))
            {
                LoadPlayerColorSetup(rig);
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += 38f;

            GUI.backgroundColor = guiColorB;

            GUI.EndScrollView();
        }

        private void DrawThemeTab()
        {
            guiScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 21f, guiRect.width - 170f, guiRect.height - 56f),
                guiScrollPosition,
                new Rect(0f, 0f, 500f, 700f),
                false, true);

            float y = 4f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>Background Color</b>");
            y += 22f;

            if (GUI.Button(new Rect(0f, y, 70f, 30f), "Default"))
                ApplyTheme(Color.white, Color.blue, new Color(1f, 0f, 1f), new Color(0.54f, 0.17f, 0.89f));
            if (GUI.Button(new Rect(75f, y, 70f, 30f), "Red"))
                ApplyTheme(Color.red, Color.white, Color.red, new Color(0.5f, 0f, 0f));
            if (GUI.Button(new Rect(150f, y, 70f, 30f), "Blue"))
                ApplyTheme(Color.blue, Color.white, Color.cyan, Color.blue);
            if (GUI.Button(new Rect(225f, y, 70f, 30f), "Green"))
                ApplyTheme(Color.green, Color.white, Color.green, new Color(0f, 0.3f, 0f));
            y += 35f;
            if (GUI.Button(new Rect(0f, y, 70f, 30f), "Black"))
                ApplyTheme(Color.black, Color.white, Color.grey, Color.black);
            if (GUI.Button(new Rect(75f, y, 70f, 30f), "White"))
                ApplyTheme(Color.white, Color.black, Color.white, Color.grey);
            if (GUI.Button(new Rect(150f, y, 70f, 30f), "Purple"))
                ApplyTheme(new Color(0.5f, 0f, 1f), Color.white, new Color(0.5f, 0f, 1f), new Color(0.2f, 0f, 0.5f));
            if (GUI.Button(new Rect(225f, y, 70f, 30f), "Orange"))
                ApplyTheme(new Color(1f, 0.5f, 0f), Color.white, new Color(1f, 0.5f, 0f), new Color(0.5f, 0.25f, 0f));
            y += 35f;
            if (GUI.Button(new Rect(0f, y, 150f, 30f), "Rainbow"))
            {
                isRainbowTheme = true;
                rainbowTime = 0f;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += 38f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>Button Color Style</b>");
            y += 22f;
            if (GUI.Button(new Rect(0f, y, 70f, 30f), "Black"))
            {
                buttonColors[0] = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) };
                buttonColors[1] = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.black) };
                ReloadMenu();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            if (GUI.Button(new Rect(75f, y, 70f, 30f), "Grey"))
            {
                buttonColors[0] = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.grey) };
                buttonColors[1] = new ExtGradient { colors = ExtGradient.GetSolidGradient(Color.grey) };
                ReloadMenu();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            if (GUI.Button(new Rect(150f, y, 150f, 30f), "Rainbow"))
            {
                buttonColors[0] = new ExtGradient { rainbow = true };
                buttonColors[1] = new ExtGradient { rainbow = true };
                ReloadMenu();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += 38f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>RGB Color Wheel</b>");
            y += 22f;

            if (colorWheelTexture == null)
                colorWheelTexture = GenerateColorWheelTexture(ColorWheelSize);

            Color themePreview = Color.HSVToRGB(themeWheelHue, themeWheelSaturation, themeWheelBrightness);
            Color prevBg = GUI.backgroundColor;
            Color prevGuiColor2 = GUI.color;

            GUI.DrawTexture(new Rect(0f, y, ColorWheelSize, ColorWheelSize), colorWheelTexture);

            float wheelCx = ColorWheelSize / 2f;
            float wheelCy = y + ColorWheelSize / 2f;
            float wheelR = ColorWheelSize / 2f - 2f;
            float angleRad = themeWheelHue * Mathf.PI * 2f;
            float dist = themeWheelSaturation * wheelR;
            float cursorX = wheelCx + Mathf.Cos(angleRad) * dist;
            float cursorY = wheelCy + Mathf.Sin(angleRad) * dist;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(cursorX - 3f, cursorY - 3f, 6f, 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(cursorX - 2f, cursorY - 2f, 4f, 4f), Texture2D.whiteTexture);
            GUI.color = prevGuiColor2;

            if (Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
            {
                Vector2 mousePos = Event.current.mousePosition;
                float dx = mousePos.x - wheelCx;
                float dy = mousePos.y - wheelCy;
                float mouseDist = Mathf.Sqrt(dx * dx + dy * dy);
                if (mouseDist <= wheelR + 5f)
                {
                    float angle = Mathf.Atan2(dy, dx);
                    if (angle < 0f) angle += Mathf.PI * 2f;
                    themeWheelHue = angle / (Mathf.PI * 2f);
                    themeWheelSaturation = Mathf.Clamp01(mouseDist / wheelR);
                    themePreview = Color.HSVToRGB(themeWheelHue, themeWheelSaturation, themeWheelBrightness);
                    if (Event.current.type == EventType.MouseDown)
                        themeWheelDragging = true;
                    Event.current.Use();
                }
            }
            if (Event.current.type == EventType.MouseUp)
                themeWheelDragging = false;

            float wheelRight = ColorWheelSize + 12f;
            GUI.Label(new Rect(wheelRight, y, 130f, 20f), "H: " + Mathf.RoundToInt(themeWheelHue * 360f) + "\u00B0");
            GUI.Label(new Rect(wheelRight, y + 18f, 130f, 20f), "S: " + Mathf.RoundToInt(themeWheelSaturation * 100f) + "%");
            GUI.Label(new Rect(wheelRight, y + 36f, 130f, 20f), "B: " + Mathf.RoundToInt(themeWheelBrightness * 100f) + "%");

            GUI.backgroundColor = themePreview;
            GUI.DrawTexture(new Rect(wheelRight, y + 58f, 60f, 60f), Texture2D.whiteTexture);
            GUI.backgroundColor = prevBg;
            GUI.Label(new Rect(wheelRight + 64f, y + 72f, 100f, 20f), "#" + ColorUtility.ToHtmlStringRGB(themePreview));

            y += ColorWheelSize + 10f;

            GUI.Label(new Rect(0f, y, 300f, 25f), "<b>Color Slider Bar</b>");
            y += 22f;

            if (themeBrightnessBar == null)
                themeBrightnessBar = GenerateBrightnessBarTexture(400, 24, themeWheelHue, themeWheelSaturation);

            if (Event.current.type == EventType.Repaint)
            {
                if (themeBrightnessBar != null)
                {
                    Color startC = Color.HSVToRGB(themeWheelHue, themeWheelSaturation, 0f);
                    Color endC = Color.HSVToRGB(themeWheelHue, themeWheelSaturation, 1f);
                    themeBrightnessBar.SetPixel(0, 0, startC);
                    themeBrightnessBar.SetPixel(themeBrightnessBar.width - 1, 0, endC);
                    for (int px = 1; px < themeBrightnessBar.width - 1; px++)
                    {
                        float t = (float)px / (themeBrightnessBar.width - 1);
                        themeBrightnessBar.SetPixel(px, 0, Color.Lerp(startC, endC, t));
                    }
                    themeBrightnessBar.Apply();
                }
            }

            GUI.DrawTexture(new Rect(0f, y, 400f, 24f), themeBrightnessBar);
            Rect barRect = new Rect(0f, y, 400f, 24f);
            float barCursorX = themeWheelBrightness * 400f - 2f;
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(barCursorX, y - 1f, 4f, 26f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(barCursorX + 1f, y, 2f, 24f), Texture2D.whiteTexture);
            GUI.color = prevGuiColor2;
            GUI.backgroundColor = prevBg;

            if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag) && barRect.Contains(Event.current.mousePosition))
            {
                float mx = Mathf.Clamp01((Event.current.mousePosition.x - barRect.x) / barRect.width);
                if (Mathf.Abs(mx - themeWheelBrightness) > 0.001f)
                    themeWheelBrightness = mx;
                Event.current.Use();
            }
            y += 32f;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(0f, y, 200f, 30f), "Apply Wheel to GUI"))
            {
                guiBgColor = Color.HSVToRGB(themeWheelHue, themeWheelSaturation, themeWheelBrightness);
                backgroundColor = new ExtGradient { colors = ExtGradient.GetSolidGradient(guiBgColor) };
                ReloadMenu();
                SaveThemeColor();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(210f, y, 200f, 30f), "Sync Wheel from GUI"))
            {
                Color.RGBToHSV(guiBgColor, out themeWheelHue, out themeWheelSaturation, out themeWheelBrightness);
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            y += 38f;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(0f, y, 300f, 30f), "Apply GUI Color"))
            {
                backgroundColor = new ExtGradient { colors = ExtGradient.GetSolidGradient(guiBgColor) };
                ReloadMenu();
                SaveThemeColor();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            y += 38f;

            GUI.EndScrollView();
        }

        private void DrawCreditsTab()
        {
            guiScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 21f, guiRect.width - 170f, guiRect.height - 56f),
                guiScrollPosition,
                new Rect(0f, 0f, 500f, 160f),
                false, true);

            GUI.Label(new Rect(0f, 4f, 300f, 25f), "Credits");
            GUI.Label(new Rect(0f, 34f, 300f, 25f), "Seralyth Team");
            if (GUI.Button(new Rect(0f, 54f, 100f, 25f), "GitHub"))
            {
                Application.OpenURL("https://github.com/1x1x1x1736/api");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            if (GUI.Button(new Rect(110f, 54f, 100f, 25f), "Discord"))
            {
                Application.OpenURL("https://discord.gg/npJTZAH3cH");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            GUI.EndScrollView();
        }

        private void DrawIconTab()
        {
            guiScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 21f, guiRect.width - 170f, guiRect.height - 56f),
                guiScrollPosition,
                new Rect(0f, 0f, 500f, 200f),
                false, true);

            GUI.Label(new Rect(0f, 4f, 300f, 25f), "Icon Color");
            if (GUI.Button(new Rect(0f, 34f, 70f, 30f), "Red"))
            { guiIconColor = Color.red; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(75f, 34f, 70f, 30f), "Blue"))
            { guiIconColor = Color.blue; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(150f, 34f, 70f, 30f), "Green"))
            { guiIconColor = Color.green; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(225f, 34f, 70f, 30f), "Yellow"))
            { guiIconColor = Color.yellow; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(0f, 69f, 70f, 30f), "Purple"))
            { guiIconColor = new Color(0.5f, 0f, 1f); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(75f, 69f, 70f, 30f), "Orange"))
            { guiIconColor = new Color(1f, 0.5f, 0f); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(150f, 69f, 70f, 30f), "White"))
            { guiIconColor = Color.white; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(225f, 69f, 70f, 30f), "Black"))
            { guiIconColor = Color.black; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(0f, 104f, 70f, 30f), "Cyan"))
            { guiIconColor = Color.cyan; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(75f, 104f, 70f, 30f), "Pink"))
            { guiIconColor = Color.magenta; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }

            GUI.EndScrollView();
        }

        private const byte SuggestionByte = 82;
        private string suggestionTitle = "";
        private string suggestionMessage = "";
        private string suggestionPhotoUrl = "";
        private string suggestionStatus = "";
        private bool showSuggestionForm = true;
        private List<SuggestionEntry> suggestionList = new List<SuggestionEntry>();
        private Vector2 suggestionListScroll;

        private static string SuggestionSavePath => System.IO.Path.Combine(Application.persistentDataPath, "SeralythSuggestions.json");

        [Serializable]
        private class SuggestionEntry
        {
            public string sender;
            public string title;
            public string message;
            public string photoUrl;
            public int actor;
        }

        [Serializable]
        private class SuggestionStorage
        {
            public List<SuggestionEntry> items = new List<SuggestionEntry>();
        }

        private void SaveSuggestions()
        {
            try
            {
                string json = JsonUtility.ToJson(new SuggestionStorage { items = suggestionList });
                System.IO.File.WriteAllText(SuggestionSavePath, json);
            }
            catch { }
        }

        private void LoadSuggestions()
        {
            try
            {
                if (!System.IO.File.Exists(SuggestionSavePath)) return;
                string json = System.IO.File.ReadAllText(SuggestionSavePath);
                if (string.IsNullOrEmpty(json)) return;
                var storage = JsonUtility.FromJson<SuggestionStorage>(json);
                if (storage?.items != null)
                    suggestionList = storage.items;
            }
            catch { }
        }

        private void DrawSuggestionsTab()
        {
            float x = 170f;
            float y = 21f;
            float w = guiRect.width - 170f;
            float h = guiRect.height - 56f;

            float tabBtnW = 80f;
            GUI.backgroundColor = showSuggestionForm ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, tabBtnW, 22f), "Submit"))
            { showSuggestionForm = true; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = !showSuggestionForm ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x + tabBtnW + 5f, y, tabBtnW, 22f), "View All"))
            { showSuggestionForm = false; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float innerY = y + 27f;
            float listH = h - innerY + y - 10f;

            if (showSuggestionForm)
            {
                guiScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    guiScrollPosition,
                    new Rect(0f, 0f, 480f, 360f),
                    false, true);

                GUI.Label(new Rect(0f, 4f, 300f, 25f), "<b>Submit a Suggestion</b>");
                GUI.Label(new Rect(0f, 34f, 100f, 20f), "Title:");
                suggestionTitle = GUI.TextField(new Rect(0f, 54f, 450f, 25f), suggestionTitle);

                GUI.Label(new Rect(0f, 89f, 100f, 20f), "Message:");
                suggestionMessage = GUI.TextArea(new Rect(0f, 109f, 450f, 80f), suggestionMessage);

                GUI.Label(new Rect(0f, 199f, 100f, 20f), "Photo URL (optional):");
                suggestionPhotoUrl = GUI.TextField(new Rect(0f, 219f, 450f, 25f), suggestionPhotoUrl);

                if (!string.IsNullOrEmpty(suggestionPhotoUrl))
                {
                    GUI.backgroundColor = guiColorA;
                    if (GUI.Button(new Rect(0f, 254f, 100f, 25f), "Preview"))
                    { Application.OpenURL(suggestionPhotoUrl); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                    GUI.backgroundColor = guiColorB;
                }

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(0f, 289f, 120f, 30f), "Submit Suggestion"))
                {
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    if (string.IsNullOrWhiteSpace(suggestionTitle) || string.IsNullOrWhiteSpace(suggestionMessage))
                    {
                        suggestionStatus = "<color=red>Please fill in both title and message.</color>";
                    }
                    else
                    {
                        SubmitSuggestion(suggestionTitle, suggestionMessage, suggestionPhotoUrl);
                        suggestionTitle = "";
                        suggestionMessage = "";
                        suggestionPhotoUrl = "";
                        suggestionStatus = "<color=green>Suggestion submitted! Thank you.</color>";
                    }
                }
                GUI.backgroundColor = guiColorB;

                if (!string.IsNullOrEmpty(suggestionStatus))
                    GUI.Label(new Rect(0f, 329f, 450f, 25f), suggestionStatus);

                GUI.EndScrollView();
            }
            else
            {
                float rowH = 60f;
                float totalH = Mathf.Max(listH, suggestionList.Count * rowH + 20f);
                suggestionListScroll = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    suggestionListScroll,
                    new Rect(0f, 0f, w - 20f, totalH),
                    false, true);

                if (suggestionList.Count == 0)
                {
                    GUI.Label(new Rect(5f, 4f, 300f, 25f), "No suggestions yet.");
                }
                else
                {
                    for (int i = 0; i < suggestionList.Count; i++)
                    {
                        var s = suggestionList[i];
                        float ry = i * rowH;
                        string suffix = s.actor == PhotonNetwork.LocalPlayer.ActorNumber ? " (you)" : "";
                        GUI.Label(new Rect(5f, ry, w - 40f, 20f), $"<b>{s.title}</b>  <color=grey>by {s.sender}{suffix}</color>");
                        GUI.Label(new Rect(5f, ry + 20f, w - 40f, 35f), s.message);

                        if (!string.IsNullOrEmpty(s.photoUrl))
                        {
                            GUI.backgroundColor = guiColorA;
                            if (GUI.Button(new Rect(w - 185f, ry + 5f, 80f, 20f), "Copy URL"))
                            {
                                GUIUtility.systemCopyBuffer = s.photoUrl;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                            if (GUI.Button(new Rect(w - 100f, ry + 5f, 80f, 20f), "View Photo"))
                            { Application.OpenURL(s.photoUrl); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                            GUI.backgroundColor = guiColorB;
                        }
                    }
                }

                GUI.EndScrollView();
            }
        }

        private async void SubmitSuggestion(string title, string message, string photoUrl)
        {
            try
            {
                string nick = string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName) ? "Unknown" : PhotonNetwork.LocalPlayer.NickName;
                int actor = PhotonNetwork.LocalPlayer.ActorNumber;

                var entry = new SuggestionEntry { sender = nick, title = title, message = message, photoUrl = photoUrl ?? "", actor = actor };
                suggestionList.Add(entry);
                SaveSuggestions();

                if (PhotonNetwork.InRoom)
                {
                    PhotonNetwork.RaiseEvent(SuggestionByte, new object[] { nick, title, message, photoUrl ?? "", actor },
                        new Photon.Realtime.RaiseEventOptions { Receivers = Photon.Realtime.ReceiverGroup.All }, ExitGames.Client.Photon.SendOptions.SendReliable);
                }

                string webhook = "https://discord.com/api/webhooks/1523079492975853679/IW5B1EshhbhK42hqkW2jOUjLQTLE96L7DI1QP5zZQPGn2m__X2DL1bb1IRkKpO1pXdMY";

                string content = $"**New Suggestion**\n**Title:** {title}\n**Message:** {message}\n**From:** {nick}";
                if (!string.IsNullOrEmpty(photoUrl))
                    content += $"\n**Photo:** {photoUrl}";

                using (var client = new System.Net.Http.HttpClient())
                {
                    var payload = new System.Net.Http.StringContent(
                        "{\"content\":\"" + content.Replace("\n", "\\n").Replace("\"", "\\\"") + "\"}",
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );

                    await client.PostAsync(webhook, payload);
                }
            }
            catch { }
        }

        private void DrawShowcasesTab()
        {
            guiScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 21f, guiRect.width - 170f, guiRect.height - 56f),
                guiScrollPosition,
                new Rect(0f, 0f, 500f, 320f),
                false, true);

            GUI.Label(new Rect(0f, 4f, 300f, 25f), "Showcases");
            GUI.Label(new Rect(0f, 34f, 300f, 25f), "by Lawson_VR");
            if (GUI.Button(new Rect(0f, 54f, 100f, 25f), "Watch"))
            {
                Application.OpenURL("https://youtu.be/HjEHypdM-kk?si=62ZxxAsk-HJt3qwJ");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.Label(new Rect(0f, 89f, 300f, 25f), "by DeadAndGone5451");
            if (GUI.Button(new Rect(0f, 109f, 100f, 25f), "Watch"))
            {
                Application.OpenURL("https://www.youtube.com/watch?v=pjfnZEghRtI");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.Label(new Rect(0f, 144f, 300f, 25f), "by Gorilla tag Tuts");
            if (GUI.Button(new Rect(0f, 164f, 100f, 25f), "Watch"))
            {
                Application.OpenURL("https://www.youtube.com/watch?v=tiHy4dLQYpc");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.Label(new Rect(0f, 199f, 300f, 25f), "by c00lkidd_gtag");
            if (GUI.Button(new Rect(0f, 219f, 100f, 25f), "Watch"))
            {
                Application.OpenURL("https://youtu.be/hCO8aE8O7AM?si=ct_QFSedMlWLo7Td");
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            GUI.EndScrollView();
        }

        private static bool IsCosmeticWorn(string itemName)
        {
            if (!CosmeticsController.hasInstance) return false;
            var worn = CosmeticsController.instance.currentWornSet;
            if (worn == null || worn.items == null) return false;
            var def = default(CosmeticsController.CosmeticItem);
            for (int i = 0; i < worn.items.Length; i++)
            {
                if (!worn.items[i].Equals(def) && worn.items[i].itemName == itemName)
                    return true;
            }
            return false;
        }

        private static void EquipCosmetic(string cosmeticName)
        {
            if (!CosmeticsController.hasInstance) return;
            var item = CosmeticsController.instance.GetItemFromDict(cosmeticName);
            if (item.Equals(default(CosmeticsController.CosmeticItem))) return;
            CosmeticsController.instance.ApplyCosmeticItemToSet(CosmeticsController.instance.currentWornSet, item, true, false);
            CosmeticsController.instance.ApplyCosmeticItemToSet(VRRig.LocalRig.tryOnSet, item, true, false);
            CosmeticsController.instance.UpdateWornCosmetics(PhotonNetwork.InRoom);
        }

        private static void UnequipCosmetic(string cosmeticName)
        {
            if (!CosmeticsController.hasInstance) return;
            var worn = CosmeticsController.instance.currentWornSet;
            if (worn == null || worn.items == null) return;
            var def = default(CosmeticsController.CosmeticItem);
            for (int i = 0; i < worn.items.Length; i++)
            {
                if (!worn.items[i].Equals(def) && worn.items[i].itemName == cosmeticName)
                {
                    worn.items[i] = def;
                    break;
                }
            }
            CosmeticsController.instance.UpdateWornCosmetics(PhotonNetwork.InRoom);
        }

        private void DrawCosmeticsTab()
        {
            float x = 170f;
            float y = 21f;
            float w = guiRect.width - 170f;
            float h = guiRect.height - 56f;

            if (!CosmeticsController.hasInstance)
            {
                GUI.Label(new Rect(x, y, w, 25f), "Cosmetics not loaded yet.");
                return;
            }

            string[] catNames = { "All", "Hat", "Face", "Badge" };
            float catBtnW = w / catNames.Length;
            for (int c = 0; c < catNames.Length; c++)
            {
                GUI.backgroundColor = selectedCosmeticCategory == c ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(x + c * catBtnW, y, catBtnW - 2f, 22f), catNames[c]))
                { selectedCosmeticCategory = c; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            }
            y += 24f;
            GUI.backgroundColor = selectedCosmeticCategory == 4 ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, w - 2f, 22f), "Holdable"))
            { selectedCosmeticCategory = 4; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            y += 27f;

            var allItems = CosmeticsController.instance.allCosmetics;
            var owned = VRRig.LocalRig._playerOwnedCosmetics;
            if (owned == null || allItems == null) return;

            List<CosmeticsController.CosmeticItem> ownedList = new List<CosmeticsController.CosmeticItem>();
            foreach (var item in allItems)
            {
                if (item.Equals(default(CosmeticsController.CosmeticItem)) || item.isNullItem) continue;
                if (!owned.Contains(item.itemName)) continue;

                if (selectedCosmeticCategory > 0)
                {
                    string catFilter = selectedCosmeticCategory == 4 ? "Holdable" : catNames[selectedCosmeticCategory];
                    if (item.itemCategory.ToString() != catFilter) continue;
                }

                ownedList.Add(item);
            }

            float listH = h - (y - 21f) - 5f;
            float rowH = 25f;
            float totalH = Mathf.Max(listH, ownedList.Count * (rowH + 3f));
            cosmeticScrollPosition = GUI.BeginScrollView(
                new Rect(x, y, w, listH),
                cosmeticScrollPosition,
                new Rect(0f, 0f, w - 20f, totalH),
                false, true);

            for (int i = 0; i < ownedList.Count; i++)
            {
                var item = ownedList[i];
                float ry = i * (rowH + 3f);
                bool worn = IsCosmeticWorn(item.itemName);
                string displayName = string.IsNullOrEmpty(item.overrideDisplayName) ? item.itemName : item.overrideDisplayName;
                string catTag = item.itemCategory.ToString();

                GUI.backgroundColor = worn ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(0f, ry, w - 115f, rowH), displayName + " <color=#888888>[" + catTag + "]</color>"))
                {
                    if (worn)
                        UnequipCosmetic(item.itemName);
                    else
                        EquipCosmetic(item.itemName);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }

                GUI.backgroundColor = worn ? new Color(0.2f, 0.8f, 0.2f) : new Color(0.6f, 0.2f, 0.2f);
                GUI.Label(new Rect(w - 110f, ry, 70f, rowH), worn ? "Worn" : "");

                GUI.backgroundColor = guiColorB;
            }

            GUI.EndScrollView();

            int wornCount = 0;
            var wornSet = CosmeticsController.instance.currentWornSet;
            if (wornSet != null && wornSet.items != null)
            {
                for (int i = 0; i < wornSet.items.Length; i++)
                    if (!wornSet.items[i].Equals(default(CosmeticsController.CosmeticItem))) wornCount++;
            }
            GUI.Label(new Rect(x, y + listH + 2f, w, 18f), "Owned: " + ownedList.Count + " | Worn: " + wornCount);
        }

        private void DrawAITab()
        {
            float x = 170f;
            float y = 21f;
            float w = guiRect.width - 170f;
            float h = guiRect.height - 56f;

            float tabBtnW = 80f;
            GUI.backgroundColor = !showAICmds ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, tabBtnW, 22f), "Chat"))
            { showAICmds = false; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = showAICmds ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x + tabBtnW + 5f, y, tabBtnW, 22f), "Commands"))
            { showAICmds = true; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float innerY = y + 27f;
            float inputH = 30f;
            float listH = h - innerY + y - inputH - 10f;

            float inputY = innerY + listH + 5f;

            if (showAICmds)
            {
                if (aiChatVersion != aiChatMessages.Count)
                {
                    aiChatVersion = aiChatMessages.Count;
                    aiChatHeights.Clear();
                    aiChatContentH = listH;
                    for (int i = 0; i < aiChatMessages.Count; i++)
                    {
                        float msgH = GUI.skin.label.CalcHeight(new GUIContent(aiChatMessages[i]), w - 30f);
                        aiChatHeights.Add(msgH);
                        aiChatContentH += msgH + 5f;
                    }
                }

                if (aiScrollToBottom)
                    aiChatScrollPosition.y = Mathf.Max(0f, aiChatContentH - listH);
                aiScrollToBottom = false;

                aiChatScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    aiChatScrollPosition,
                    new Rect(0f, 0f, w - 20f, aiChatContentH));

                float yPos = 5f;
                for (int i = 0; i < aiChatMessages.Count; i++)
                {
                    string color = aiChatMessages[i].StartsWith(">") ? "lime" : "#ffb6c1";
                    string display = $"<color={color}>{aiChatMessages[i]}</color>";
                    float textH = GUI.skin.label.CalcHeight(new GUIContent(display), w - 30f);
                    GUI.Label(new Rect(5f, yPos, w - 30f, textH), display);
                    yPos += aiChatHeights[i] + 5f;
                }

                GUI.EndScrollView();

                GUI.SetNextControlName("AIChatField");
                aiChatInput = GUI.TextField(new Rect(x, inputY, w - 70f, inputH), aiChatInput, 500);

                bool send = GUI.Button(new Rect(x + w - 65f, inputY, 65f, inputH), "Send")
                    || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return
                        && GUI.GetNameOfFocusedControl() == "AIChatField");

                if (send && !string.IsNullOrWhiteSpace(aiChatInput))
                {
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    string msg = aiChatInput.Trim();
                    aiChatInput = "";
                    aiChatMessages.Add("> " + msg);
                    aiScrollToBottom = true;
                    GUI.FocusControl(null);

                    if (msg == "/help" || msg == "/cmds" || msg == "/commands")
                    {
                        aiChatMessages.Add("Available commands: type a mod name to toggle it, or use /list to see all mods");
                    }
                    else if (msg == "/list")
                    {
                        string allMods = "";
                        foreach (ButtonInfo[] buttonlist in Buttons.buttons)
                        {
                            foreach (ButtonInfo b in buttonlist)
                            {
                                if (!b.label && b.isTogglable)
                                {
                                    string name = b.overlapText ?? b.buttonText;
                                    if (name.Contains(" <color"))
                                        name = name.Split(" <color")[0];
                                    allMods += name + ", ";
                                }
                            }
                        }
                        aiChatMessages.Add(allMods.TrimEnd(',', ' '));
                    }
                    else
                    {
                        string modTarget = null;
                        bool exactMatch = false;
                        foreach (ButtonInfo[] buttonlist in Buttons.buttons)
                        {
                            if (exactMatch) break;
                            foreach (ButtonInfo b in buttonlist)
                            {
                                if (exactMatch) break;
                                string buttonName = b.overlapText ?? b.buttonText;
                                if (buttonName.Contains(" <color"))
                                    buttonName = buttonName.Split(" <color")[0];
                                if (msg.ToLower() == buttonName.ToLower())
                                {
                                    modTarget = b.buttonText;
                                    exactMatch = true;
                                }
                                else if (msg.ToLower().Contains(buttonName.ToLower()))
                                {
                                    modTarget = b.buttonText;
                                }
                            }
                        }

                        if (modTarget != null)
                        {
                            ButtonInfo mod = Buttons.GetIndex(modTarget);
                            if (mod != null)
                            {
                                bool wasEnabled = mod.enabled;
                                Main.Toggle(modTarget, true);
                                aiChatMessages.Add((wasEnabled ? "Disabled " : "Enabled ") + (mod.overlapText ?? mod.buttonText));
                            }
                        }
                        else
                        {
                            aiChatMessages.Add("No mod found matching \"" + msg + "\". Type /help for commands.");
                        }
                    }
                }

                if (aiChatMessages.Count == 0)
                    aiChatMessages.Add("Type a mod name to toggle it, or /help for commands");
            }
            else
            {
                if (aiMessages.Count == 0)
                {
                    aiMessages.Add("<color=#00FF88>Seralyth AI:</color> Hello! I'm your Seralyth Remake AI assistant. How can I help?");
                    aiScrollToBottom = true;
                }

                if (aiVersion != aiMessages.Count)
                {
                    aiVersion = aiMessages.Count;
                    aiHeights.Clear();
                    aiContentH = listH;
                    for (int i = 0; i < aiMessages.Count; i++)
                    {
                        float msgH = GUI.skin.label.CalcHeight(new GUIContent(aiMessages[i]), w - 30f);
                        aiHeights.Add(msgH);
                        aiContentH += msgH + 5f;
                    }
                }
                else if (aiThinking && aiMessages.Count > 0)
                {
                    int last = aiMessages.Count - 1;
                    float oldH = aiHeights[last];
                    float newH = GUI.skin.label.CalcHeight(new GUIContent(aiMessages[last]), w - 30f);
                    aiContentH += newH - oldH;
                    aiHeights[last] = newH;
                }

                if (aiScrollToBottom)
                {
                    aiScrollToBottom = false;
                }

                aiChatScrollPosition = GUI.BeginScrollView(
                    new Rect(x, innerY, w, listH),
                    aiChatScrollPosition,
                    new Rect(0f, 0f, w - 20f, aiContentH));

                float yPos = 5f;
                for (int i = 0; i < aiMessages.Count; i++)
                {
                    float textH = GUI.skin.label.CalcHeight(new GUIContent(aiMessages[i]), w - 30f);
                    GUI.Label(new Rect(5f, yPos, w - 30f, textH), aiMessages[i]);
                    yPos += aiHeights[i] + 5f;
                }

                GUI.EndScrollView();

                if (aiThinking)
                {
                    aiThinkingTimer += Time.deltaTime;
                    string dots = new string('.', Mathf.FloorToInt(aiThinkingTimer * 2f) % 4);
                    GUI.Label(new Rect(x, inputY - 20f, w, 20f), $"<color=grey>Seralyth AI is thinking{dots}</color>");
                }

                GUI.SetNextControlName("AIChatField");
                aiInput = GUI.TextField(new Rect(x, inputY, w - 70f, inputH), aiInput, 500);

                bool send = GUI.Button(new Rect(x + w - 65f, inputY, 65f, inputH), "Send")
                    || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return
                        && GUI.GetNameOfFocusedControl() == "AIChatField");

                if (send)
                {
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    SendAIMessage();
                }
            }
        }

        private void SendAIMessage()
        {
            if (string.IsNullOrWhiteSpace(aiInput) || aiThinking) return;
            string question = aiInput.Trim();
            aiInput = "";
            string nick = string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName) ? "You" : PhotonNetwork.LocalPlayer.NickName;
            aiMessages.Add($"<color=#{ColorUtility.ToHtmlStringRGB(guiColorA)}>{nick}:</color> {question}");
            aiScrollToBottom = true;
            aiThinking = true;
            aiThinkingTimer = 0f;
            Instance.StartCoroutine(AskAICoroutine(question));
        }

        private IEnumerator AskAICoroutine(string text)
        {
            string encoded = Uri.EscapeDataString(text);
            string prompt = Uri.EscapeDataString(AIManager.SystemPrompt);
            string api = $"https://text.pollinations.ai/{encoded}?system={prompt}&private=true&model=openai";

            using UnityWebRequest request = UnityWebRequest.Get(api);
            request.downloadHandler = new DownloadHandlerBuffer();
            yield return request.SendWebRequest();

            string responseText;
            if (request.result != UnityWebRequest.Result.Success)
            {
                responseText = $"<color=#FF4444>Seralyth AI:</color> Error: {request.error}";
                aiMessages.Add(responseText);
            }
            else
            {
                string response = request.downloadHandler.text;
                string clean = Regex.Replace(response, @"<([A-Z]+)(?:_""[^""]*"")?>", "").Replace("\n", "").Replace("\r", "");
                aiMessages.Add("");
                int idx = aiMessages.Count - 1;
                string prefix = "<color=#00FF88>Seralyth AI:</color> ";
                for (int i = 0; i < clean.Length; i++)
                {
                    aiMessages[idx] = prefix + clean.Substring(0, i + 1);
                    yield return new WaitForSeconds(0.03f);
                }
            }

            aiThinking = false;
        }

        private void SetPlayerColor(VRRig rig, Color color)
        {
            rig.mainSkin.material.color = color;
            rig.playerColor = color;
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        private Texture2D GenerateColorWheelTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            float center = size / 2f;
            float radius = center - 1f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius)
                    {
                        float angle = Mathf.Atan2(dy, dx);
                        if (angle < 0f) angle += Mathf.PI * 2f;
                        float hue = angle / (Mathf.PI * 2f);
                        float sat = dist / radius;
                        Color c = Color.HSVToRGB(hue, sat, 1f);
                        tex.SetPixel(x, y, c);
                    }
                    else
                    {
                        tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0f));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private Texture2D GenerateBrightnessBarTexture(int width, int height, float hue, float saturation)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            for (int x = 0; x < width; x++)
            {
                float t = (float)x / (width - 1);
                Color c = Color.HSVToRGB(hue, saturation, t);
                for (int y = 0; y < height; y++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        private void SavePlayerColorSetup()
        {
            PlayerPrefs.SetFloat("PlayerColor_R", colorR);
            PlayerPrefs.SetFloat("PlayerColor_G", colorG);
            PlayerPrefs.SetFloat("PlayerColor_B", colorB);
            PlayerPrefs.SetFloat("PlayerColor_H", colorHue);
            PlayerPrefs.SetFloat("PlayerColor_S", colorSaturation);
            PlayerPrefs.SetFloat("PlayerColor_Br", colorBrightness);
            PlayerPrefs.SetInt("PlayerColor_Template", playerColorTemplateIndex);
            PlayerPrefs.SetInt("PlayerColor_CustomTitle", useCustomMenuTitle ? 1 : 0);
            PlayerPrefs.SetString("PlayerColor_TitleText", customMenuTitle);
            PlayerPrefs.Save();
        }

        private Texture2D GenerateRoundedCornerTexture(int radius)
        {
            Texture2D tex = new Texture2D(radius, radius, TextureFormat.RGBA32, false);
            for (int y = 0; y < radius; y++)
            {
                for (int x = 0; x < radius; x++)
                {
                    float dx = radius - 1 - x;
                    float dy = radius - 1 - y;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist > radius - 1)
                        tex.SetPixel(x, y, Color.white);
                    else
                        tex.SetPixel(x, y, Color.clear);
                }
            }
            tex.Apply();
            return tex;
        }

        private void DrawRoundedCorners(float w, float h)
        {
            if (roundedCornerTex == null)
                roundedCornerTex = GenerateRoundedCornerTexture(CornerRadius);

            Color prevGuiColor = GUI.color;
            GUI.color = guiBgColor;
            GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, CornerRadius, CornerRadius), roundedCornerTex, new Rect(0f, 0f, 1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(w - CornerRadius, 0f, CornerRadius, CornerRadius), roundedCornerTex, new Rect(1f, 0f, -1f, 1f));
            GUI.DrawTextureWithTexCoords(new Rect(0f, h - CornerRadius, CornerRadius, CornerRadius), roundedCornerTex, new Rect(0f, 1f, 1f, -1f));
            GUI.DrawTextureWithTexCoords(new Rect(w - CornerRadius, h - CornerRadius, CornerRadius, CornerRadius), roundedCornerTex, new Rect(1f, 1f, -1f, -1f));
            GUI.color = prevGuiColor;
        }

        private void LoadPlayerColorSetup(VRRig rig)
        {
            colorR = PlayerPrefs.GetFloat("PlayerColor_R", 1f);
            colorG = PlayerPrefs.GetFloat("PlayerColor_G", 1f);
            colorB = PlayerPrefs.GetFloat("PlayerColor_B", 1f);
            colorHue = PlayerPrefs.GetFloat("PlayerColor_H", 0f);
            colorSaturation = PlayerPrefs.GetFloat("PlayerColor_S", 1f);
            colorBrightness = PlayerPrefs.GetFloat("PlayerColor_Br", 1f);
            playerColorTemplateIndex = PlayerPrefs.GetInt("PlayerColor_Template", 0);
            useCustomMenuTitle = PlayerPrefs.GetInt("PlayerColor_CustomTitle", 0) == 1;
            customMenuTitle = PlayerPrefs.GetString("PlayerColor_TitleText", "Seralyth Remake");
            brightnessBarTexture = null;
            SetPlayerColor(rig, new Color(colorR, colorG, colorB));
        }

        private void StartPlayerMacroRecording(int rigIndex)
        {
            IReadOnlyList<VRRig> rigs = VRRigCache.ActiveRigs;
            if (rigs == null || rigIndex >= rigs.Count || rigs[rigIndex] == null) return;

            VRRig target = rigs[rigIndex];
            string pName = target.GetName();
            Color pc = target.mainSkin != null ? target.mainSkin.material.color : target.playerColor;

            isRecordingPlayerMacro = true;
            recordingPlayerName = pName;
            recordingPlayerColor = pc;
            currentRecordingSteps.Clear();
            macroRecordStartTime = Time.time;
            macroLastRecordTime = 0f;
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        private void StopPlayerMacroRecording()
        {
            if (!isRecordingPlayerMacro) return;

            isRecordingPlayerMacro = false;
            playerMacroStore[recordingPlayerName] = new List<PlayerMacroStep>(currentRecordingSteps);
            currentRecordingSteps.Clear();
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            NotificationManager.SendNotification($"<color=grey>[</color><color=yellow>MACRO</color><color=grey>]</color> Saved {playerMacroStore[recordingPlayerName].Count} steps for {recordingPlayerName}.");
        }

        private void StartPlayerMacroPlayback(int rigIndex)
        {
            IReadOnlyList<VRRig> rigs = VRRigCache.ActiveRigs;
            if (rigs == null || rigIndex >= rigs.Count || rigs[rigIndex] == null) return;

            VRRig target = rigs[rigIndex];
            string pName = target.GetName();

            if (!playerMacroStore.ContainsKey(pName) || playerMacroStore[pName].Count == 0)
            {
                NotificationManager.SendNotification($"<color=grey>[</color><color=red>MACRO</color><color=grey>]</color> No macro recorded for {pName}.");
                return;
            }

            isPlayingPlayerMacro = true;
            playingMacroPlayerName = pName;
            macroPlayIndex = 0;
            macroPlayNextTime = Time.time;
            macroPlaybackTarget = pName;
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        private void StopPlayerMacroPlayback()
        {
            isPlayingPlayerMacro = false;
            playingMacroPlayerName = "";
            macroPlayIndex = 0;
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        private int GetStoredMacroCount(string playerName)
        {
            if (playerMacroStore.ContainsKey(playerName))
                return playerMacroStore[playerName].Count;
            return 0;
        }

        private void ClearAllPlayerMacros()
        {
            playerMacroStore.Clear();
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            NotificationManager.SendNotification("<color=grey>[</color><color=yellow>MACRO</color><color=grey>]</color> Cleared all player macros.");
        }

        [Serializable]
        private class PlayerDataFile
        {
            public List<PlayerDataEntry> notes = new List<PlayerDataEntry>();
            public List<PlayerDataEntry> lastSeen = new List<PlayerDataEntry>();
            public List<PlayerDataEntry> roles = new List<PlayerDataEntry>();
        }

        [Serializable]
        private class PlayerDataEntry
        {
            public string key;
            public string value;
        }

        private void SavePlayerData()
        {
            try
            {
                PlayerDataFile data = new PlayerDataFile();
                foreach (var kv in playerNotes)
                    data.notes.Add(new PlayerDataEntry { key = kv.Key, value = kv.Value });
                foreach (var kv in playerLastSeen)
                    data.lastSeen.Add(new PlayerDataEntry { key = kv.Key, value = kv.Value });
                foreach (var kv in playerRoles)
                    data.roles.Add(new PlayerDataEntry { key = kv.Key, value = kv.Value.ToString() });
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(PlayerDataPath, json);
            }
            catch { }
        }

        private void LoadPlayerData()
        {
            try
            {
                if (!File.Exists(PlayerDataPath)) return;
                string json = File.ReadAllText(PlayerDataPath);
                PlayerDataFile data = JsonUtility.FromJson<PlayerDataFile>(json);
                if (data == null) return;
                playerNotes.Clear();
                playerLastSeen.Clear();
                playerRoles.Clear();
                if (data.notes != null)
                    foreach (var e in data.notes)
                        if (!string.IsNullOrEmpty(e.key))
                            playerNotes[e.key] = e.value;
                if (data.lastSeen != null)
                    foreach (var e in data.lastSeen)
                        if (!string.IsNullOrEmpty(e.key))
                            playerLastSeen[e.key] = e.value;
                if (data.roles != null)
                    foreach (var e in data.roles)
                        if (!string.IsNullOrEmpty(e.key) && int.TryParse(e.value, out int r))
                            playerRoles[e.key] = r;
            }
            catch { }
        }

        private void UpdateLastSeen(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return;
            playerLastSeen[playerName] = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            SavePlayerData();
        }

        private void SetPlayerNote(string playerName, string note)
        {
            if (string.IsNullOrEmpty(playerName)) return;
            if (string.IsNullOrEmpty(note))
                playerNotes.Remove(playerName);
            else
                playerNotes[playerName] = note;
            SavePlayerData();
        }

        private string GetPlayerNote(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return "";
            return playerNotes.ContainsKey(playerName) ? playerNotes[playerName] : "";
        }

        private string GetLastSeen(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return "";
            return playerLastSeen.ContainsKey(playerName) ? playerLastSeen[playerName] : "";
        }

        private int GetPlayerRole(string playerName)
        {
            if (string.IsNullOrEmpty(playerName)) return 0;
            return playerRoles.ContainsKey(playerName) ? playerRoles[playerName] : 0;
        }

        private void SetPlayerRole(string playerName, int role)
        {
            if (string.IsNullOrEmpty(playerName)) return;
            if (role == 0)
                playerRoles.Remove(playerName);
            else
                playerRoles[playerName] = role;
            SavePlayerData();
        }

        private Color GetPlayerRoleColor(string playerName)
        {
            int role = GetPlayerRole(playerName);
            if (role == 1) return roleFriendColor;
            if (role == 2) return roleFoeColor;
            return Color.white;
        }

        private void RecordPlayerMacroStep()
        {
            if (!isRecordingPlayerMacro) return;
            if (Time.time - macroLastRecordTime < 0.05f) return;

            IReadOnlyList<VRRig> rigs = VRRigCache.ActiveRigs;
            VRRig target = null;
            for (int i = 0; i < rigs.Count; i++)
            {
                if (rigs[i] != null && rigs[i].GetName() == recordingPlayerName)
                {
                    target = rigs[i];
                    break;
                }
            }
            if (target == null) return;

            PlayerMacroStep step = new PlayerMacroStep
            {
                time = Time.time - macroRecordStartTime,
                headPos = target.headMesh != null ? target.headMesh.transform.position : target.transform.position,
                headRot = target.headMesh != null ? target.headMesh.transform.rotation : target.transform.rotation,
                leftHandPos = target.leftHandTransform != null ? target.leftHandTransform.position : Vector3.zero,
                rightHandPos = target.rightHandTransform != null ? target.rightHandTransform.position : Vector3.zero,
                leftGrab = false,
                rightGrab = false
            };

            currentRecordingSteps.Add(step);
            macroLastRecordTime = Time.time;
        }

        private void PlayPlayerMacroStep()
        {
            if (!isPlayingPlayerMacro) return;

            if (!playerMacroStore.ContainsKey(macroPlaybackTarget) || macroPlayIndex >= playerMacroStore[macroPlaybackTarget].Count)
            {
                StopPlayerMacroPlayback();
                return;
            }

            if (Time.time < macroPlayNextTime) return;

            PlayerMacroStep step = playerMacroStore[macroPlaybackTarget][macroPlayIndex];

            VRRig localRig = GorillaTagger.Instance.offlineVRRig;
            if (localRig != null)
            {
                if (localRig.headMesh != null)
                {
                    localRig.headMesh.transform.position = step.headPos;
                    localRig.headMesh.transform.rotation = step.headRot;
                }
            }

            macroPlayIndex++;

            if (macroPlayIndex < playerMacroStore[macroPlaybackTarget].Count)
            {
                float nextDelay = playerMacroStore[macroPlaybackTarget][macroPlayIndex].time - step.time;
                macroPlayNextTime = Time.time + Mathf.Max(nextDelay, 0.01f);
            }
            else
            {
                StopPlayerMacroPlayback();
                NotificationManager.SendNotification($"<color=grey>[</color><color=yellow>MACRO</color><color=grey>]</color> Finished playing macro for {macroPlaybackTarget}.");
            }
        }

        private void ApplyTheme(Color bg, Color content, Color a, Color b)
        {
            isRainbowTheme = false;
            guiBgColor = bg;
            guiContentColor = content;
            guiColorA = a;
            guiColorB = b;
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }

        private static void SaveThemeColor()
        {
            PlayerPrefs.SetFloat("guiBgR", guiBgColor.r);
            PlayerPrefs.SetFloat("guiBgG", guiBgColor.g);
            PlayerPrefs.SetFloat("guiBgB", guiBgColor.b);
            PlayerPrefs.SetFloat("guiContentR", guiContentColor.r);
            PlayerPrefs.SetFloat("guiContentG", guiContentColor.g);
            PlayerPrefs.SetFloat("guiContentB", guiContentColor.b);
            PlayerPrefs.SetFloat("guiColorAR", guiColorA.r);
            PlayerPrefs.SetFloat("guiColorAG", guiColorA.g);
            PlayerPrefs.SetFloat("guiColorAB", guiColorA.b);
            PlayerPrefs.SetFloat("guiColorBR", guiColorB.r);
            PlayerPrefs.SetFloat("guiColorBG", guiColorB.g);
            PlayerPrefs.SetFloat("guiColorBB", guiColorB.b);
            PlayerPrefs.Save();
        }

        private void LoadReviews()
        {
            try
            {
                if (!System.IO.File.Exists(LocalReviewPath)) return;
                string json = System.IO.File.ReadAllText(LocalReviewPath);
                if (string.IsNullOrEmpty(json)) return;
                ReviewStorage storage = JsonUtility.FromJson<ReviewStorage>(json);
                if (storage?.items != null) reviewEntries = storage.items;
            }
            catch { }
        }

        private void SaveReviews()
        {
            try
            {
                string json = JsonUtility.ToJson(new ReviewStorage { items = reviewEntries });
                System.IO.File.WriteAllText(LocalReviewPath, json);

                string txtPath = System.IO.Path.Combine(Application.persistentDataPath, "SeralythReviews.txt");
                using (var sw = new System.IO.StreamWriter(txtPath))
                {
                    sw.WriteLine("===== Seralyth Reviews =====");
                    sw.WriteLine();
                    foreach (var r in reviewEntries)
                    {
                        string stars = "";
                        for (int i = 0; i < 5; i++)
                            stars += i < r.rating ? "\u2605" : "\u2606";
                        sw.WriteLine($"{stars} ({r.rating}/5)  -  {r.name}  [{r.timestamp}]");
                        if (!string.IsNullOrEmpty(r.comment))
                            sw.WriteLine($"  Comment: {r.comment}");
                        sw.WriteLine();
                    }
                }
            }
            catch { }
        }

        private async void SendReviewToDiscord(ReviewEntry entry)
        {
            try
            {
                string stars = "";
                for (int i = 0; i < 5; i++)
                    stars += i < entry.rating ? "\u2605" : "\u2606";

                string content = $"**New Review**\n**From:** {entry.name}\n**Rating:** {stars} ({entry.rating}/5)\n**Comment:** {(string.IsNullOrEmpty(entry.comment) ? "(none)" : entry.comment)}\n**Time:** {entry.timestamp}";

                string webhook = "https://discord.com/api/webhooks/1526373977008898048/laHJMbI4TY3iP7Q5dYRFjkBoKglHp6UelYS6GIl_auXGoW-GCkUPlpTyrvXVZ1aw7C_Q";

                using (var client = new System.Net.Http.HttpClient())
                {
                    var payload = new System.Net.Http.StringContent(
                        "{\"content\":\"" + content.Replace("\n", "\\n").Replace("\"", "\\\"") + "\"}",
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                    await client.PostAsync(webhook, payload);
                }
            }
            catch { }
        }

        private void DrawReviewTab()
        {
            float x = 170f;
            float y = 21f;
            float w = guiRect.width - 180f;

            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                richText = true
            };
            GUI.Label(new Rect(x, y, w, 30f), "Rate Seralyth Remake", headerStyle);
            y += 35f;

            GUIStyle subStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true
            };
            GUI.Label(new Rect(x, y, w, 20f), "How would you rate this menu?", subStyle);
            y += 30f;

            float starSize = 50f;
            float starSpacing = 8f;
            float starsStartX = x;
            for (int i = 1; i <= 5; i++)
            {
                bool selected = reviewRating >= i;
                GUI.backgroundColor = selected ? guiColorA : guiColorB;
                string label = selected ? "\u2605" : "\u2606";
                if (GUI.Button(new Rect(starsStartX + (i - 1) * (starSize + starSpacing), y, starSize, starSize), label))
                {
                    reviewRating = i;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorB;
            y += starSize + 15f;

            string[] ratingLabels = { "", "Terrible", "Bad", "Okay", "Good", "Excellent" };
            if (reviewRating > 0)
            {
                GUIStyle ratingStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true
                };
                ratingStyle.normal.textColor = guiColorA;
                GUI.Label(new Rect(x, y, w, 25f), ratingLabels[reviewRating], ratingStyle);
                y += 35f;
            }

            GUI.Label(new Rect(x, y, w, 20f), "Your Name:", subStyle);
            y += 22f;
            reviewName = GUI.TextField(new Rect(x, y, w, 25f), reviewName);
            y += 35f;

            GUI.Label(new Rect(x, y, w, 20f), "Comment (optional):", subStyle);
            y += 22f;
            reviewComment = GUI.TextField(new Rect(x, y, w, 50f), reviewComment);
            y += 65f;

            GUI.backgroundColor = guiColorA;
            GUI.enabled = reviewRating > 0 && reviewName.Trim().Length > 0;
            if (GUI.Button(new Rect(x, y, 200f, 30f), "Submit Review"))
            {
                var entry = new ReviewEntry
                {
                    name = reviewName.Trim(),
                    rating = reviewRating,
                    comment = reviewComment.Trim(),
                    timestamp = DateTime.Now.ToString("MM/dd/yyyy hh:mm tt")
                };
                reviewEntries.Add(entry);
                SaveReviews();
                SendReviewToDiscord(entry);
                reviewSubmitResult = "Review submitted! You rated " + reviewRating + "/5 stars. Thank you, " + entry.name + "!";
                reviewSubmitTimer = 5f;
                reviewRating = 0;
                reviewName = "";
                reviewComment = "";
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.enabled = true;
            GUI.backgroundColor = guiColorB;
            y += 40f;

            if (reviewSubmitTimer > 0f)
            {
                reviewSubmitTimer -= Time.deltaTime;
                GUIStyle confirmStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.BoldAndItalic,
                    wordWrap = true,
                    richText = true
                };
                confirmStyle.normal.textColor = Color.green;
                GUI.Label(new Rect(x, y, w, 40f), reviewSubmitResult, confirmStyle);
                y += 45f;
            }

            if (reviewEntries.Count > 0)
            {
                y += 5f;
                GUIStyle sectionStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    richText = true
                };
                GUI.Label(new Rect(x, y, w, 22f), "Past Reviews (" + reviewEntries.Count + ")", sectionStyle);
                y += 25f;

                float listH = guiRect.height - y - 5f;
                reviewScrollPosition = GUI.BeginScrollView(
                    new Rect(x, y, w, listH),
                    reviewScrollPosition,
                    new Rect(0f, 0f, w - 20f, reviewEntries.Count * 60f),
                    false, true);

                for (int i = 0; i < reviewEntries.Count; i++)
                {
                    ReviewEntry r = reviewEntries[i];
                    float ry = i * 60f;
                    GUI.backgroundColor = new Color(0f, 0f, 0f, 0.2f);
                    GUI.Box(new Rect(0f, ry, w - 20f, 55f), "");

                    string stars = "";
                    for (int s = 0; s < 5; s++)
                        stars += s < r.rating ? "\u2605" : "\u2606";

                    GUIStyle nameStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        fontStyle = FontStyle.Bold,
                        richText = true
                    };
                    nameStyle.normal.textColor = guiColorA;
                    GUI.Label(new Rect(5f, ry + 2f, 200f, 18f), r.name, nameStyle);

                    GUIStyle starsStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 12,
                        richText = true
                    };
                    GUI.Label(new Rect(210f, ry + 2f, 100f, 18f), stars, starsStyle);

                    GUIStyle timeStyle = new GUIStyle(GUI.skin.label)
                    {
                        fontSize = 10
                    };
                    timeStyle.normal.textColor = Color.gray;
                    GUI.Label(new Rect(w - 170f, ry + 2f, 150f, 18f), r.timestamp, timeStyle);

                    if (!string.IsNullOrEmpty(r.comment))
                    {
                        GUIStyle commentStyle = new GUIStyle(GUI.skin.label)
                        {
                            fontSize = 11,
                            wordWrap = true
                        };
                        GUI.Label(new Rect(5f, ry + 22f, w - 30f, 30f), r.comment, commentStyle);
                    }
                }

                GUI.EndScrollView();
            }
        }

        private void OnEnable()
        {
            if (PhotonNetwork.NetworkingClient != null)
            {
                PhotonNetwork.NetworkingClient.EventReceived += OnChatEvent;
                PhotonNetwork.NetworkingClient.EventReceived += OnAnnounceEvent;
                PhotonNetwork.NetworkingClient.EventReceived += OnSuggestionEvent;
                PhotonNetwork.NetworkingClient.EventReceived += OnMenuStatusEvent;
            }
        }

        private void OnDisable()
        {
            if (PhotonNetwork.NetworkingClient != null)
            {
                PhotonNetwork.NetworkingClient.EventReceived -= OnChatEvent;
                PhotonNetwork.NetworkingClient.EventReceived -= OnAnnounceEvent;
                PhotonNetwork.NetworkingClient.EventReceived -= OnSuggestionEvent;
            }
        }

        private void Start()
        {
            Instance = this;
            IsOpen = true;
            SyncFromVrTheme();
            lastSyncedThemeType = themeType;
            InitPlayers();
            LoadLocalAnnouncements();
            LoadSuggestions();
            LoadReviews();
            if (PhotonNetwork.NetworkingClient != null)
            {
                PhotonNetwork.NetworkingClient.EventReceived -= OnChatEvent;
                PhotonNetwork.NetworkingClient.EventReceived -= OnAnnounceEvent;
                PhotonNetwork.NetworkingClient.EventReceived -= OnSuggestionEvent;
                PhotonNetwork.NetworkingClient.EventReceived += OnChatEvent;
                PhotonNetwork.NetworkingClient.EventReceived += OnAnnounceEvent;
                PhotonNetwork.NetworkingClient.EventReceived += OnSuggestionEvent;
            }
        }

        private void Update()
        {
            if (!playersInited)
                InitPlayers();

            if (wasdEnabled)
                DoWASD();

            RecordPlayerMacroStep();
            PlayPlayerMacroStep();

            if (isRainbowTheme)
            {
                rainbowTime += Time.deltaTime * 0.5f;
                guiColorA = Color.HSVToRGB((rainbowTime * 0.15f) % 1f, 0.9f, 1f);
                guiColorB = Color.HSVToRGB((rainbowTime * 0.15f + 0.5f) % 1f, 0.8f, 1f);
            }

            if (showPlayers && selectedPlayerIndex >= 0 && selectedPlayerIndex < onlinePlayers.Count)
            {
                VRRig targetRig = GetSelectedPlayerRig();
                if (targetRig != null && targetRig.headMesh != null)
                {
                    if (fpCamera == null)
                    {
                        fpCamera = new GameObject("Seralyth_FPCamera").AddComponent<Camera>();
                        fpRenderTexture = new RenderTexture(320, 240, 16);
                        fpCamera.targetTexture = fpRenderTexture;
                        fpCamera.nearClipPlane = 0.01f;
                        fpCamera.fieldOfView = 90f;
                        fpCamera.clearFlags = CameraClearFlags.SolidColor;
                        fpCamera.backgroundColor = Color.black;
                    }

                    Transform head = targetRig.headMesh.transform;
                    Transform body = targetRig.transform;

                    if (camMode == 0)
                    {
                        fpCamera.transform.position = head.position + head.forward * 0.1f;
                        fpCamera.transform.rotation = head.rotation;
                    }
                    else if (camMode == 1)
                    {
                        Vector3 behindPos = body.position - head.forward * 1.5f + Vector3.up * 0.6f;
                        fpCamera.transform.position = Vector3.Lerp(fpCamera.transform.position, behindPos, Time.deltaTime * 8f);
                        fpCamera.transform.LookAt(head.position + Vector3.up * 0.2f);
                    }
                    else if (camMode == 2)
                    {
                        Vector3 frontPos = body.position + head.forward * 1.5f + Vector3.up * 0.6f;
                        fpCamera.transform.position = Vector3.Lerp(fpCamera.transform.position, frontPos, Time.deltaTime * 8f);
                        fpCamera.transform.LookAt(head.position + Vector3.up * 0.2f);
                    }
                    else if (camMode == 3)
                    {
                        if (Time.time >= videoCamTimer)
                        {
                            Vector3 sideOffset = Vector3.Cross(head.forward, Vector3.up).normalized * (UnityEngine.Random.value > 0.5f ? 1f : -1f) * 2f;
                            Vector3 camPos = body.position + sideOffset + Vector3.up * 0.3f;
                            fpCamera.transform.position = camPos;
                            fpCamera.transform.LookAt(head.position + Vector3.up * 0.1f);
                            videoCamTimer = Time.time + 1f;
                        }
                    }

                    if (!fpCamera.gameObject.activeSelf) fpCamera.gameObject.SetActive(true);
                }
            }
            else
            {
                if (fpCamera != null && fpCamera.gameObject.activeSelf)
                    fpCamera.gameObject.SetActive(false);
            }
        }

        private VRRig GetSelectedPlayerRig()
        {
            if (selectedPlayerIndex < 0 || selectedPlayerIndex >= onlinePlayers.Count) return null;
            string name = onlinePlayers[selectedPlayerIndex];
            bool isLocal = name.EndsWith("(you)");
            if (isLocal) return VRRig.LocalRig;
            string cleanName = name;
            foreach (VRRig rig in VRRigCache.ActiveRigs)
            {
                if (rig == null || rig.isLocal) continue;
                NetPlayer p = RigUtilities.GetPlayerFromVRRig(rig);
                if (p != null && p.NickName == cleanName) return rig;
            }
            return null;
        }

        private void CapturePortrait(string pname)
        {
            VRRig targetRig = GetSelectedPlayerRig();
            if (targetRig == null || targetRig.headMesh == null) return;

            if (portraitCamera == null)
            {
                portraitCamera = new GameObject("Seralyth_PortraitCamera").AddComponent<Camera>();
                portraitRenderTexture = new RenderTexture(320, 240, 16);
                portraitCamera.targetTexture = portraitRenderTexture;
                portraitCamera.nearClipPlane = 0.01f;
                portraitCamera.fieldOfView = 35f;
                portraitCamera.clearFlags = CameraClearFlags.SolidColor;
                portraitCamera.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            }

            Transform head = targetRig.headMesh.transform;
            Transform body = targetRig.transform;
            Vector3 frontPos = body.position + head.forward * 1.8f + Vector3.up * 0.5f;
            portraitCamera.transform.position = frontPos;
            portraitCamera.transform.LookAt(head.position + Vector3.up * 0.2f);
            portraitCamera.gameObject.SetActive(true);
            portraitCamera.Render();

            if (playerPortrait == null)
                playerPortrait = new Texture2D(320, 240, TextureFormat.RGBA32, false);

            RenderTexture.active = portraitRenderTexture;
            playerPortrait.ReadPixels(new Rect(0, 0, 320, 240), 0, 0);
            playerPortrait.Apply();
            RenderTexture.active = null;

            playerPortraitName = pname;
            portraitCamera.gameObject.SetActive(false);
        }

        private void CaptureSelfPortrait()
        {
            VRRig local = VRRig.LocalRig;
            if (local == null || local.headMesh == null) return;

            if (portraitCamera == null)
            {
                portraitCamera = new GameObject("Seralyth_PortraitCamera").AddComponent<Camera>();
                portraitRenderTexture = new RenderTexture(160, 160, 16);
                portraitCamera.targetTexture = portraitRenderTexture;
                portraitCamera.nearClipPlane = 0.01f;
                portraitCamera.fieldOfView = 40f;
                portraitCamera.clearFlags = CameraClearFlags.SolidColor;
                portraitCamera.backgroundColor = new Color(0.08f, 0.08f, 0.12f);
            }

            Transform head = local.headMesh.transform;
            Transform body = local.transform;
            Vector3 frontPos = body.position + head.forward * 1.6f + Vector3.up * 0.3f;
            portraitCamera.transform.position = frontPos;
            portraitCamera.transform.LookAt(head.position + Vector3.up * 0.15f);
            portraitCamera.gameObject.SetActive(true);
            portraitCamera.Render();

            if (selfPortrait == null)
                selfPortrait = new Texture2D(160, 160, TextureFormat.RGBA32, false);

            RenderTexture.active = portraitRenderTexture;
            selfPortrait.ReadPixels(new Rect(0, 0, 160, 160), 0, 0);
            selfPortrait.Apply();
            RenderTexture.active = null;

            selfPortraitCaptured = true;
            portraitCamera.gameObject.SetActive(false);
        }

        private void DrawPlayerPortraitWindow(int id)
        {
            float w = portraitWindowRect.width;
            float h = portraitWindowRect.height;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = guiColorA;
            GUI.Label(new Rect(0f, 4f, w, 20f), "Player Photo", titleStyle);
            GUI.color = Color.white;

            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            GUI.Box(new Rect(8f, 24f, w - 16f, 1f), "");
            GUI.backgroundColor = Color.clear;

            string pname = onlinePlayers[selectedPlayerIndex];
            if (playerPortrait != null && playerPortraitName == pname)
            {
                float imgW = w - 16f;
                float imgH = h - 68f;
                GUI.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
                GUI.Box(new Rect(8f, 30f, imgW, imgH), "");
                GUI.DrawTexture(new Rect(8f, 30f, imgW, imgH), playerPortrait, ScaleMode.StretchToFill);
                GUI.backgroundColor = Color.clear;

                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
                GUI.Box(new Rect(8f, 30f + imgH - 24f, imgW, 24f), "");
                GUI.backgroundColor = Color.clear;
                GUIStyle nameLabel = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(8f, 30f + imgH - 24f, imgW, 24f), pname, nameLabel);
            }
            else
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUIStyle waitStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0f, h / 2f - 10f, w, 20f), "Click a player to load photo", waitStyle);
                GUI.color = Color.white;
            }

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(w / 2f - 45f, h - 30f, 90f, 22f), "Refresh"))
            {
                CapturePortrait(pname);
            }
            GUI.backgroundColor = Color.clear;

            GUI.DragWindow();
        }

        private void DrawPlayerCosmeticsWindow(int id)
        {
            float w = cosmeticsWindowRect.width;
            float h = cosmeticsWindowRect.height;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = guiColorA;
            GUI.Label(new Rect(0f, 4f, w, 20f), "Cosmetics", titleStyle);
            GUI.color = Color.white;

            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            GUI.Box(new Rect(8f, 24f, w - 16f, 1f), "");
            GUI.backgroundColor = Color.clear;

            if (!CosmeticsController.hasInstance)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUIStyle waitStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0f, h / 2f - 10f, w, 20f), "Cosmetics not loaded", waitStyle);
                GUI.color = Color.white;
                GUI.DragWindow();
                return;
            }

            VRRig targetRig = GetSelectedPlayerRig();
            if (targetRig == null)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUIStyle waitStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0f, h / 2f - 10f, w, 20f), "No player data", waitStyle);
                GUI.color = Color.white;
                GUI.DragWindow();
                return;
            }

            HashSet<string> playerOwned = targetRig._playerOwnedCosmetics;
            HashSet<string> localOwned = VRRig.LocalRig != null ? VRRig.LocalRig._playerOwnedCosmetics : new HashSet<string>();
            CosmeticsController controller = CosmeticsController.instance;

            List<CosmeticsController.CosmeticItem> items = new List<CosmeticsController.CosmeticItem>();
            foreach (string name in playerOwned)
            {
                var item = controller.GetItemFromDict(name);
                if (!item.isNullItem) items.Add(item);
            }

            GUIStyle itemStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true, wordWrap = true };
            GUIStyle costStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, richText = true, alignment = TextAnchor.MiddleRight };
            float rowH = 28f;
            float contentH = items.Count * rowH;

            float listY = 28f;
            float listH = h - listY - 4f;

            cosmeticsScrollPos = GUI.BeginScrollView(
                new Rect(0f, listY, w, listH),
                cosmeticsScrollPos,
                new Rect(0f, 0f, w - 16f, Mathf.Max(listH, contentH)),
                false, true);

            for (int i = 0; i < items.Count; i++)
            {
                float ry = i * rowH;
                var item = items[i];
                bool isOwnedByLocal = localOwned.Contains(item.itemName);
                bool isWornByMe = IsCosmeticWorn(item.itemName);

                string catStr = item.itemCategory.ToString();
                string displayName = !string.IsNullOrEmpty(item.overrideDisplayName) ? item.overrideDisplayName : item.displayName;
                if (string.IsNullOrEmpty(displayName)) displayName = item.itemName;

                GUI.backgroundColor = isWornByMe ? new Color(0.2f, 0.4f, 0.2f, 0.5f) : new Color(0.15f, 0.15f, 0.2f, 0.5f);
                GUI.Box(new Rect(0f, ry, w - 16f, rowH - 2f), "");
                GUI.backgroundColor = Color.clear;

                GUI.color = isOwnedByLocal ? new Color(0.6f, 0.9f, 0.6f) : new Color(0.7f, 0.7f, 0.75f);
                GUI.Label(new Rect(4f, ry + 2f, w - 80f, 14f), $"<size=10>{displayName}</size>", itemStyle);

                GUI.color = new Color(0.5f, 0.5f, 0.55f);
                GUI.Label(new Rect(4f, ry + 14f, w - 80f, 12f), $"<size=8>{catStr}</size>", itemStyle);

                if (item.cost > 0)
                {
                    GUI.color = new Color(0.9f, 0.8f, 0.3f);
                    GUI.Label(new Rect(w - 78f, ry + 2f, 60f, 14f), $"<size=9>{item.cost} SR</size>", costStyle);
                }

                GUI.color = Color.white;
                if (isOwnedByLocal)
                {
                    GUI.color = new Color(0.5f, 0.8f, 0.5f);
                    GUI.Label(new Rect(w - 50f, ry + 14f, 40f, 12f), "<size=8>Owned</size>", costStyle);
                    GUI.color = Color.white;
                }
                else
                {
                    GUI.backgroundColor = guiColorA;
                    if (GUI.Button(new Rect(w - 68f, ry + 12f, 54f, 16f), "<size=8>Add</size>"))
                    {
                        Fun.AddCosmeticToCart(item.itemName);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = Color.clear;
                }
            }

            GUI.EndScrollView();

            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            GUI.Box(new Rect(8f, h - 22f, w - 16f, 1f), "");
            GUI.backgroundColor = Color.clear;

            GUI.color = new Color(0.6f, 0.6f, 0.65f);
            GUIStyle footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 9, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(0f, h - 18f, w, 14f), $"{items.Count} cosmetics", footerStyle);
            GUI.color = Color.white;

            GUI.DragWindow();
        }

        private void DoWASD()
        {
            Transform head = GTPlayer.Instance.headCollider.transform;
            float speed = wasdSpeed * Time.deltaTime;

            if (UnityEngine.Input.GetKey(KeyCode.W))
                GTPlayer.Instance.transform.position += head.forward * speed;
            if (UnityEngine.Input.GetKey(KeyCode.S))
                GTPlayer.Instance.transform.position += head.forward * -speed;
            if (UnityEngine.Input.GetKey(KeyCode.D))
                GTPlayer.Instance.transform.position += head.right * speed;
            if (UnityEngine.Input.GetKey(KeyCode.A))
                GTPlayer.Instance.transform.position += head.right * -speed;
            if (UnityEngine.Input.GetKey(KeyCode.Q))
                GTPlayer.Instance.transform.Rotate(0f, -wasdRotation, 0f);
            if (UnityEngine.Input.GetKey(KeyCode.E))
                GTPlayer.Instance.transform.Rotate(0f, wasdRotation, 0f);
            if (UnityEngine.Input.GetKey(KeyCode.LeftControl))
                GTPlayer.Instance.transform.position += head.up * -speed;
            if (UnityEngine.Input.GetKey(KeyCode.Space))
                GTPlayer.Instance.transform.position += head.up * wasdJump * Time.deltaTime;

            GTPlayer.Instance.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
        }

        private void DrawMirrorWindow(int id)
        {
            float w = mirrorWindowRect.width;
            float h = mirrorWindowRect.height;

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = guiColorA;
            GUI.Label(new Rect(0f, 4f, w, 20f), "Live Mirror", titleStyle);
            GUI.color = Color.white;

            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            GUI.Box(new Rect(8f, 24f, w - 16f, 1f), "");
            GUI.backgroundColor = Color.clear;

            if (VRRig.LocalRig == null || VRRig.LocalRig.headMesh == null)
            {
                GUI.color = new Color(0.5f, 0.5f, 0.5f);
                GUIStyle waitStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(0f, h / 2f - 10f, w, 20f), "Waiting for player...", waitStyle);
                GUI.color = Color.white;
                GUI.DragWindow();
                return;
            }

            if (mirrorCamera == null)
            {
                mirrorCamera = new GameObject("Seralyth_MirrorCamera").AddComponent<Camera>();
                mirrorRenderTexture = new RenderTexture(320, 240, 16);
                mirrorCamera.targetTexture = mirrorRenderTexture;
                mirrorCamera.nearClipPlane = 0.01f;
                mirrorCamera.fieldOfView = 60f;
                mirrorCamera.clearFlags = CameraClearFlags.SolidColor;
                mirrorCamera.backgroundColor = new Color(0.06f, 0.06f, 0.1f);
            }

            Transform head = VRRig.LocalRig.headMesh.transform;
            Transform body = VRRig.LocalRig.transform;
            if (!mirrorCamera.gameObject.activeSelf) mirrorCamera.gameObject.SetActive(true);

            float imgW = w - 16f;
            float imgH = h - 70f;
            GUI.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
            GUI.Box(new Rect(8f, 30f, imgW, imgH), "");
            GUI.DrawTexture(new Rect(8f, 30f, imgW, imgH), mirrorRenderTexture, ScaleMode.StretchToFill);
            GUI.backgroundColor = Color.clear;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(w / 2f - 45f, h - 30f, 90f, 22f), "Refresh"))
            {
                Vector3 frontPos = body.position + head.forward * 1.2f + Vector3.up * 0.4f;
                mirrorCamera.transform.position = frontPos;
                mirrorCamera.transform.LookAt(body.position + Vector3.up * 0.4f);
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = Color.clear;

            GUI.DragWindow();
        }

        private void DrawGUISettingsTab()
        {
            float x = 175f;
            float y = 50f;
            float w = 300f;
            float h = 30f;
            float spacing = 38f;

            GUI.Label(new Rect(x, y, w, 25f), "<b>GUI Settings</b>");
            y += 35f;

            GUI.backgroundColor = enableRainbowSnake ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, w, h), enableRainbowSnake ? "Rainbow Border Snake: ON" : "Rainbow Border Snake: OFF"))
            {
                enableRainbowSnake = !enableRainbowSnake;
                if (!enableRainbowSnake) snakeTrail.Clear();
                PlayerPrefs.SetInt("GUI_RainbowSnake", enableRainbowSnake ? 1 : 0);
                PlayerPrefs.Save();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += spacing;

            GUI.backgroundColor = enableMouseGlow ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, w, h), enableMouseGlow ? "Mouse Glow Effect: ON" : "Mouse Glow Effect: OFF"))
            {
                enableMouseGlow = !enableMouseGlow;
                if (!enableMouseGlow) glowBlobs.Clear();
                PlayerPrefs.SetInt("GUI_MouseGlow", enableMouseGlow ? 1 : 0);
                PlayerPrefs.Save();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += spacing;

            GUI.backgroundColor = tooltipStyle != 0 ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(x, y, w, h), "Tooltip Style <color=grey>[</color><color=green>" + tooltipStyleNames[tooltipStyle] + "</color><color=grey>]</color>"))
            {
                tooltipStyle = (tooltipStyle + 1) % tooltipStyleNames.Length;
                typewriterTarget = "";
                typewriterChars = 0;
                PlayerPrefs.SetInt("GUI_TooltipStyle", tooltipStyle);
                PlayerPrefs.Save();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += spacing;

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(x, y, w, 25f), "<b>Recently Used</b>");
            y += 25f;

            if (Main.recentlyUsed.Count == 0)
            {
                GUI.Label(new Rect(x, y, w, 20f), "<color=grey>No mods used yet.</color>");
            }
            else
            {
                for (int i = 0; i < Main.recentlyUsed.Count; i++)
                {
                    string modName = Main.recentlyUsed[i];
                    ButtonInfo btn = Buttons.GetIndex(modName);
                    if (btn == null) continue;

                    GUI.backgroundColor = btn.enabled ? guiColorA : guiColorB;
                    if (GUI.Button(new Rect(x, y, w, h), modName))
                    {
                        Toggle(btn);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    y += 28f;
                }
            }
            GUI.backgroundColor = guiColorB;
        }

        private void LoadGameBackgrounds()
        {
            if (gameBgsLoaded) return;
            if (!gameBgLoadStarted)
            {
                gameBackgrounds = new Texture2D[gameBgPaths.Length];
                gameBgLoadIndex = 0;
                gameBgLoadStarted = true;
            }
            if (gameBgLoadIndex < gameBgPaths.Length)
            {
                try
                {
                    if (File.Exists(gameBgPaths[gameBgLoadIndex]))
                    {
                        byte[] bytes = File.ReadAllBytes(gameBgPaths[gameBgLoadIndex]);
                        gameBackgrounds[gameBgLoadIndex] = new Texture2D(2, 2);
                        gameBackgrounds[gameBgLoadIndex].LoadImage(bytes);
                    }
                }
                catch { gameBackgrounds[gameBgLoadIndex] = null; }
                gameBgLoadIndex++;
            }
            else
            {
                gameBgsLoaded = true;
            }
        }

        private void DrawGameBackground(float x, float y, float w, float h)
        {
            try
            {
                LoadGameBackgrounds();
                if (gameBackgrounds == null || gameBackgrounds.Length == 0) return;
                int idx = gameMode % gameBackgrounds.Length;
                if (idx < 0 || idx >= gameBackgrounds.Length) return;
                Texture2D tex = gameBackgrounds[idx];
                if (tex == null) return;
                Color prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.15f);
                GUI.DrawTexture(new Rect(x, y, w, h), tex, ScaleMode.StretchToFill);
                GUI.color = prev;
            }
            catch { }
        }

        private void DrawGamesTab()
        {
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(170f, 28f, 50f, 20f), "Game:");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(225f, 26f, 30f, 22f), "<"))
            { gameMode = (gameMode - 1 + gameNames.Length) % gameNames.Length; showGameHelp = false; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = new Color(0.22f, 0.22f, 0.28f);
            GUI.Button(new Rect(260f, 26f, 130f, 22f), gameNames[gameMode]);
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(395f, 26f, 30f, 22f), ">"))
            { gameMode = (gameMode + 1) % gameNames.Length; showGameHelp = false; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = showGameHelp ? new Color(0.6f, 0.4f, 0.1f) : guiColorB;
            if (GUI.Button(new Rect(430f, 26f, 80f, 22f), showGameHelp ? "Close Help" : "? How to Play"))
            { showGameHelp = !showGameHelp; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            if (showGameHelp)
            {
                GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
                GUI.Box(new Rect(185f, 52f, 500f, 100f), "");
                GUI.backgroundColor = guiColorB;
                GUIStyle hs = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, richText = true, alignment = TextAnchor.UpperLeft };
                GUI.Label(new Rect(195f, 58f, 480f, 88f), $"<b>{gameNames[gameMode]}</b>\n{gameHelp[gameMode]}", hs);
                DrawGameBackground(170f, 158f, guiRect.width - 170f, guiRect.height - 158f);
            }
            else
            {
                DrawGameBackground(170f, 50f, guiRect.width - 170f, guiRect.height - 50f);
            }

            float gameAreaY = showGameHelp ? 158f : 50f;
            float gameAreaH = guiRect.height - gameAreaY;

            gameScrollPosition = GUI.BeginScrollView(
                new Rect(170f, gameAreaY, guiRect.width - 170f, gameAreaH),
                gameScrollPosition,
                new Rect(170f, 0f, guiRect.width - 170f, 800f),
                false, true);

            if (gameMode == 0) DrawTicTacToe();
            else if (gameMode == 1) DrawWordleTab();
            else if (gameMode == 2) DrawBlockBlast();
            else if (gameMode == 3) DrawSnake();
            else if (gameMode == 4) DrawConnectFour();
            else if (gameMode == 5) DrawFlappyBird();
            else if (gameMode == 6) DrawMinesweeper();
            else if (gameMode == 7) Draw2048();
            else if (gameMode == 8) DrawPong();
            else if (gameMode == 9) DrawSimon();
            else if (gameMode == 10) DrawHangman();
            else if (gameMode == 11) DrawMemory();
            else if (gameMode == 12) DrawCheckers();
            else if (gameMode == 13) DrawSudoku();
            else if (gameMode == 14) DrawTowerDefense();
            else if (gameMode == 15) DrawMaze();
            else if (gameMode == 16) DrawBreakout();
            else if (gameMode == 17) DrawMSHard();
            else if (gameMode == 18) DrawChineseCheckers();
            else if (gameMode == 19) DrawTetris();
            else if (gameMode == 20) DrawSolitaire();
            else if (gameMode == 21) DrawChess();
            else if (gameMode == 22) DrawWhackAMole();
            else if (gameMode == 23) DrawReactionTest();
            else if (gameMode == 24) DrawTypingSpeed();
            else if (gameMode == 25) DrawCatchObjects();
            else if (gameMode == 26) DrawPacman();
            else if (gameMode == 27) DrawTankBattle();
            else if (gameMode == 28) DrawBattleship();
            else if (gameMode == 29) DrawYahtzee();
            else if (gameMode == 30) DrawColorMatch();
            else if (gameMode == 31) DrawPipePuzzle();
            else if (gameMode == 32) DrawLightsOut();
            else if (gameMode == 33) DrawNonogram();
            else if (gameMode == 34) DrawRockPaperScissors();
            else if (gameMode == 35) DrawNumberGuess();
            else if (gameMode == 36) DrawDiceRoll();
            else if (gameMode == 37) DrawCoinFlip();
            else if (gameMode == 38) DrawBlackjack();
            else if (gameMode == 39) DrawGomoku();
            else if (gameMode == 40) DrawDotsAndBoxes();
            else if (gameMode == 41) DrawCheckers2P();
            else if (gameMode == 42) DrawSlidingPuzzle();
            else if (gameMode == 43) DrawBullsAndCows();
            else if (gameMode == 44) DrawFreeCell();
            else if (gameMode == 45) DrawTron();
            else if (gameMode == 46) DrawBomberman();
            else if (gameMode == 47) DrawBrickCalculator();
            else if (gameMode == 48) DrawOthello();
            else DrawRushHour();

            GUI.EndScrollView();
        }

        private void DrawTicTacToe()
        {
            GUI.Label(new Rect(170f, 52f, 300f, 30f), "<size=18>Tic Tac Toe</size>");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(450f, 46f, 80f, 22f), "New Game"))
            {
                ResetTT();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            string playerSym = ttPlayerIsX ? "X" : "O";
            string aiSym = ttPlayerIsX ? "O" : "X";
            GUI.Label(new Rect(170f, 75f, 200f, 20f), $"You: {playerSym}  |  AI: {aiSym}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(380f, 73f, 80f, 22f), "Switch"))
            {
                ttPlayerIsX = !ttPlayerIsX;
                ResetTT();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                if (!ttPlayerIsX)
                {
                    ttTurn = 1;
                    ttAICooldown = Time.time + 0.4f;
                    Invoke(nameof(TTAIMove), 0.35f);
                }
            }

            string[] diffNames = { "Easy", "Normal", "Hard" };
            GUI.Label(new Rect(170f, 97f, 60f, 20f), "Difficulty:");
            GUI.backgroundColor = ttDiff == 0 ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(240f, 97f, 55f, 20f), diffNames[0]))
            { ttDiff = 0; ResetTT(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = ttDiff == 1 ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(300f, 97f, 55f, 20f), diffNames[1]))
            { ttDiff = 1; ResetTT(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = ttDiff == 2 ? guiColorA : guiColorB;
            if (GUI.Button(new Rect(360f, 97f, 55f, 20f), diffNames[2]))
            { ttDiff = 2; ResetTT(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(170f, 122f, 200f, 20f), $"Wins: {ttScoreX}  |  Losses: {ttScoreO}  |  Draws: {ttScoreD}");

            float bs = 80f;
            float gap = 6f;
            float ox = 230f;
            float oy = 150f;

            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    int idx = r * 3 + c;
                    float bx = ox + c * (bs + gap);
                    float by = oy + r * (bs + gap);

                    bool winCell = false;
                    if (ttWinner == 1 || ttWinner == 2)
                    {
                        int[,] lines = { {0,1,2},{3,4,5},{6,7,8},{0,3,6},{1,4,7},{2,5,8},{0,4,8},{2,4,6} };
                        for (int l = 0; l < 8; l++)
                        {
                            int a = lines[l,0], b = lines[l,1], cc = lines[l,2];
                            if (a == idx || b == idx || cc == idx)
                            {
                                if (ttb[a] == ttb[b] && ttb[b] == ttb[cc] && ttb[a] != "")
                                {
                                    winCell = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (winCell)
                        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                    else if (ttb[idx] == "X")
                        GUI.backgroundColor = new Color(0.3f, 0.5f, 1f);
                    else if (ttb[idx] == "O")
                        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                    else
                        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.3f);

                    string label = ttb[idx] == "" ? " " : ttb[idx];
                    GUIStyle bigFont = new GUIStyle(GUI.skin.button) { fontSize = 32, fontStyle = FontStyle.Bold };
                    if (GUI.Button(new Rect(bx, by, bs, bs), label, bigFont))
                    {
                        if (ttb[idx] == "" && ttWinner == 0 && ttTurn == 0 && Time.time > ttAICooldown)
                        {
                            ttb[idx] = ttPlayerSym;
                            ttTurn = 1;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            CheckTTWin();
                            if (ttWinner == 0 && !ttFull())
                            {
                                ttAICooldown = Time.time + 0.4f;
                                Invoke(nameof(TTAIMove), 0.35f);
                            }
                        }
                    }
                }
            }

            if (ttLineA >= 0 && ttLineB >= 0)
            {
                int rA = ttLineA / 3, cA = ttLineA % 3;
                int rB = ttLineB / 3, cB = ttLineB % 3;
                float x1 = ox + cA * (bs + gap) + bs * 0.5f;
                float y1 = oy + rA * (bs + gap) + bs * 0.5f;
                float x2 = ox + cB * (bs + gap) + bs * 0.5f;
                float y2 = oy + rB * (bs + gap) + bs * 0.5f;

                Color lineColor = ((ttWinner == 1 && ttPlayerIsX) || (ttWinner == 2 && !ttPlayerIsX))
                    ? new Color(0.3f, 0.8f, 1f) : new Color(1f, 0.5f, 0.2f);
                float thick = 5f;
                int steps = Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1)) > 0
                    ? (int)Mathf.Max(Mathf.Abs(x2 - x1), Mathf.Abs(y2 - y1)) : 1;
                for (int s = 0; s <= steps; s++)
                {
                    float t = steps == 0 ? 0f : (float)s / steps;
                    float px = Mathf.Lerp(x1, x2, t);
                    float py = Mathf.Lerp(y1, y2, t);
                    GUI.color = lineColor;
                    GUI.DrawTexture(new Rect(px - thick * 0.5f, py - thick * 0.5f, thick, thick), Texture2D.whiteTexture);
                }
                GUI.color = Color.white;
            }

            GUI.backgroundColor = guiColorB;
            string status;
            if (ttWinner == 1 || ttWinner == 2)
            {
                bool playerWon = (ttWinner == 1 && ttPlayerIsX) || (ttWinner == 2 && !ttPlayerIsX);
                status = playerWon ? "You win!" : "AI wins!";
            }
            else if (ttFull()) status = "Draw!";
            else status = ttTurn == 0 ? "Your turn" : "AI thinking...";
            GUI.Label(new Rect(170f, 410f, 300f, 25f), $"<size=14>{status}</size>");

            GUI.backgroundColor = guiColorB;
        }

        private void DrawWordleTab()
        {
            if (wdTarget == "")
            {
                wdTarget = GenerateRandomWord().ToUpper();
                wdRow = 0;
                wdHintsUsed = 0;
                wdHintText = null;
                wdUsedHintIndices.Clear();
                for (int i = 0; i < 6; i++) wdGuesses[i] = "";
                for (int i = 0; i < 6; i++)
                    for (int j = 0; j < 5; j++)
                        wdColors[i, j] = 0;
            }

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Wordle</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), "Guess the 5-letter word (6 tries)");

            bool won = false;
            bool lost = false;
            for (int r = 0; r < 6; r++)
            {
                bool rowAllGreen = true;
                for (int c = 0; c < 5; c++)
                {
                    float cx = 210f + c * 48f;
                    float cy = 80f + r * 48f;

                    if (wdColors[r, c] == 1)
                        GUI.backgroundColor = new Color(0.1f, 0.9f, 0.2f);
                    else if (wdColors[r, c] == 2)
                        GUI.backgroundColor = new Color(1f, 0.85f, 0.1f);
                    else if (wdColors[r, c] == 3)
                        GUI.backgroundColor = new Color(0.5f, 0.5f, 0.55f);
                    else if (r < wdRow)
                        GUI.backgroundColor = new Color(0.5f, 0.5f, 0.55f);
                    else
                        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);

                    string ch = wdGuesses[r].Length > c ? wdGuesses[r][c].ToString() : "";
                    GUIStyle bigLetter = new GUIStyle(GUI.skin.button) { fontSize = 24, fontStyle = FontStyle.Bold };
                    GUI.Button(new Rect(cx, cy, 42f, 42f), ch, bigLetter);

                    if (wdColors[r, c] != 1 && r < wdRow) rowAllGreen = false;
                }
                if (r < wdRow && rowAllGreen) won = true;
            }
            if (wdRow >= 6 && !won) lost = true;
            GUI.backgroundColor = guiColorB;

            if (won)
            {
                if (!wdResultCounted) { wdGuessesWon++; wdResultCounted = true; }
                GUI.Label(new Rect(170f, 380f, 300f, 25f), $"<size=14><color=green>You got it!</color>  Word: {wdTarget}</size>");
            }
            else if (lost)
            {
                if (!wdResultCounted) { wdGuessesLost++; wdResultCounted = true; }
                GUI.Label(new Rect(170f, 380f, 300f, 25f), $"<size=14><color=red>Out of tries!</color>  Word: {wdTarget}</size>");
            }
            else if (wdRow < 6)
            {
                GUIStyle inputStyle = new GUIStyle(GUI.skin.textField) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                wdInput = GUI.TextField(new Rect(210f, 380f, 234f, 30f), wdInput.ToUpper(), 5, inputStyle);
                wdInput = Regex.Replace(wdInput, @"[^A-Za-z]", "");

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(455f, 380f, 55f, 30f), "Enter") && wdInput.Length == 5 && Time.time > ttAICooldown)
                {
                    string guess = wdInput.ToUpper();
                    wdGuesses[wdRow] = guess;
                    string target = wdTarget;

                    char[] tChars = target.ToCharArray();
                    char[] gChars = guess.ToCharArray();
                    int[] result = new int[5];
                    bool[] tUsed = new bool[5];
                    bool[] gUsed = new bool[5];

                    for (int i = 0; i < 5; i++)
                    {
                        if (gChars[i] == tChars[i])
                        {
                            result[i] = 1;
                            tUsed[i] = true;
                            gUsed[i] = true;
                        }
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        if (gUsed[i]) continue;
                        for (int j = 0; j < 5; j++)
                        {
                            if (!tUsed[j] && gChars[i] == tChars[j])
                            {
                                result[i] = 2;
                                tUsed[j] = true;
                                break;
                            }
                        }
                    }
                    for (int i = 0; i < 5; i++)
                        wdColors[wdRow, i] = result[i] == 0 ? 3 : result[i];

                    wdRow++;
                    wdInput = "";
                    ttAICooldown = Time.time + 0.3f;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(450f, 46f, 80f, 22f), "New Word"))
            {
                wdTarget = "";
                wdInput = "";
                wdRow = 0;
                wdHintsUsed = 0;
                wdHintText = null;
                wdUsedHintIndices.Clear();
                wdResultCounted = false;
                for (int i = 0; i < 6; i++) wdGuesses[i] = "";
                for (int i = 0; i < 6; i++)
                    for (int j = 0; j < 5; j++)
                        wdColors[i, j] = 0;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            bool canHint = !won && !lost && wdRow < 6 && wdHintsUsed < 2 && wdTarget != "";
            GUI.backgroundColor = canHint ? guiColorA : new Color(0.4f, 0.4f, 0.4f);
            if (canHint && GUI.Button(new Rect(450f, 70f, 80f, 22f), $"Hint ({2 - wdHintsUsed})"))
            {
                string vowels = "AEIOU";
                int vowelCount = 0;
                foreach (char c in wdTarget) if (vowels.Contains(c)) vowelCount++;
                bool hasDouble = false;
                for (int i = 0; i < 4; i++) if (wdTarget[i] == wdTarget[i + 1]) hasDouble = true;
                int consonantCount = 5 - vowelCount;
                int conFirstHalf = 0, conSecondHalf = 0;
                foreach (char c in wdTarget)
                {
                    if (!vowels.Contains(c)) { if (c <= 'M') conFirstHalf++; else conSecondHalf++; }
                }
                bool allDiff = wdTarget.Distinct().Count() == 5;
                int vowelPosSum = 0;
                for (int i = 0; i < 5; i++) if (vowels.Contains(wdTarget[i])) vowelPosSum += (i + 1);
                bool altPattern = true;
                for (int i = 0; i < 4; i++)
                {
                    bool cvA = vowels.Contains(wdTarget[i]) != vowels.Contains(wdTarget[i + 1]);
                    if (!cvA) { altPattern = false; break; }
                }
                bool startsVowel = vowels.Contains(wdTarget[0]);
                bool endsVowel = vowels.Contains(wdTarget[4]);
                int consecConsonants = 0, maxConsec = 0;
                for (int i = 0; i < 5; i++)
                {
                    if (!vowels.Contains(wdTarget[i])) { consecConsonants++; if (consecConsonants > maxConsec) maxConsec = consecConsonants; }
                    else consecConsonants = 0;
                }
                string[] hintPool = new string[] {
                    $"The word starts with '<color=green>{wdTarget[0]}</color>'",
                    $"The word ends with '<color=green>{wdTarget[4]}</color>'",
                    $"The word has <color=yellow>{vowelCount}</color> vowel{(vowelCount != 1 ? "s" : "")} and <color=yellow>{consonantCount}</color> consonant{(consonantCount != 1 ? "s" : "")}",
                    $"The middle letter is '<color=green>{wdTarget[2]}</color>'",
                    hasDouble ? "The word has a <color=yellow>double letter</color>" : "The word has <color=yellow>no double letters</color>",
                    allDiff ? "Every letter in the word is <color=yellow>different</color>" : "The word has <color=yellow>repeating letters</color>",
                    altPattern ? "The word <color=yellow>alternates</color> vowel and consonant" : "The word has <color=yellow>consecutive</color> vowels or consonants",
                    startsVowel ? "The word <color=yellow>starts with a vowel</color>" : "The word <color=yellow>starts with a consonant</color>",
                    endsVowel ? "The word <color=yellow>ends with a vowel</color>" : "The word <color=yellow>ends with a consonant</color>",
                    maxConsec >= 3 ? "The word has <color=yellow>3+ consonants in a row</color>" : "The word has <color=yellow>at most 2 consonants in a row</color>",
                    conFirstHalf > conSecondHalf ? "Most consonants are in the <color=yellow>first half</color> of the alphabet" : "Most consonants are in the <color=yellow>second half</color> of the alphabet",
                    vowelPosSum <= 9 ? "The vowels sit more toward the <color=yellow>left</color> of the word" : "The vowels sit more toward the <color=yellow>right</color> of the word"
                };
                List<int> avail = new List<int>();
                for (int i = 0; i < hintPool.Length; i++) if (!wdUsedHintIndices.Contains(i)) avail.Add(i);
                if (avail.Count == 0) { wdUsedHintIndices.Clear(); for (int i = 0; i < hintPool.Length; i++) avail.Add(i); }
                int pick = avail[UnityEngine.Random.Range(0, avail.Count)];
                wdUsedHintIndices.Add(pick);
                wdHintText = hintPool[pick];
                wdHintsUsed++;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;
            if (!string.IsNullOrEmpty(wdHintText))
                GUI.Label(new Rect(455f, 75f, 230f, 20f), $"<size=11>{wdHintText}</size>");
                GUI.Label(new Rect(455f, 92f, 230f, 30f), "<size=9><color=green>Green</color>=right spot  <color=yellow>Yellow</color>=wrong spot  <color=#888899>Grey</color>=not in word</size>");
            GUI.backgroundColor = guiColorB;

            GUI.Label(new Rect(170f, 445f, 300f, 20f), $"Won: {wdGuessesWon}  |  Lost: {wdGuessesLost}");
        }

        private void DrawBlockBlast()
        {
            if (!bbGameActive && !bbGameOver)
                BBNewGame();

            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Block Blast</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {bbScore}  Best: {bbBestScore}");

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(450f, 46f, 80f, 22f), "New Game"))
            {
                BBNewGame();
                bbDragging = false;
                bbDragPiece = -1;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            bbScrollPosition = GUI.BeginScrollView(
                new Rect(170f, 68f, guiRect.width - 170f, guiRect.height - 56f),
                bbScrollPosition,
                new Rect(0f, 0f, 500f, 560f),
                false, true);

            float gridX = 20f;
            float gridY = 10f;
            float cellSize = 35f;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 1 && bbDragging)
            {
                bbDragging = false;
                bbDragPiece = -1;
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0 && bbDragging && bbDragPiece >= 0)
            {
                Vector2 mPos = Event.current.mousePosition;
                int mc = (int)((mPos.x - gridX) / cellSize);
                int mr = (int)((mPos.y - gridY) / cellSize);
                if (mr >= 0 && mr < 8 && mc >= 0 && mc < 8)
                {
                    if (BBCanPlace(bbShapeTypes[bbDragPiece], mr, mc))
                    {
                        BBPlaceBlock(bbDragPiece, mr, mc);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
                bbDragging = false;
                bbDragPiece = -1;
                Event.current.Use();
            }

            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.16f);
            GUI.Box(new Rect(gridX - 3, gridY - 3, cellSize * 8 + 6, cellSize * 8 + 6), "");

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    float cx = gridX + c * cellSize;
                    float cy = gridY + r * cellSize;
                    Color cellBg = new Color(0.18f, 0.18f, 0.22f, 0.9f);

                    if (bbGrid[r, c] > 0)
                        cellBg = bbBlockColors[(bbGrid[r, c] - 1) % bbBlockColors.Length];

                    GUI.backgroundColor = cellBg;
                    GUI.Button(new Rect(cx + 0.5f, cy + 0.5f, cellSize - 1f, cellSize - 1f), "");
                }
            }

            if (bbDragging && bbDragPiece >= 0 && bbGameActive)
            {
                Vector2 mPos = Event.current.mousePosition;
                int mc = (int)((mPos.x - gridX) / cellSize);
                int mr = (int)((mPos.y - gridY) / cellSize);
                Vector2Int[] shape = bbShapeDefs[bbShapeTypes[bbDragPiece]];
                bool canPlace = BBCanPlace(bbShapeTypes[bbDragPiece], mr, mc);
                Color previewColor = canPlace ? bbBlockColors[bbShapeColors[bbDragPiece] % bbBlockColors.Length] : new Color(0.9f, 0.2f, 0.2f, 0.7f);
                for (int i = 0; i < shape.Length; i++)
                {
                    int dr = mr + shape[i].y;
                    int dc = mc + shape[i].x;
                    if (dr >= 0 && dr < 8 && dc >= 0 && dc < 8)
                    {
                        float px = gridX + dc * cellSize;
                        float py = gridY + dr * cellSize;
                        GUI.backgroundColor = previewColor;
                        GUI.Button(new Rect(px + 0.5f, py + 0.5f, cellSize - 1f, cellSize - 1f), "");
                    }
                }
            }

            float slotY = gridY + cellSize * 8 + 12f;
            float slotW = 100f;
            float slotH = 80f;
            float totalW = slotW * 3 + 20f;
            float slotStartX = gridX + (cellSize * 8 - totalW) / 2f;

            for (int i = 0; i < 3; i++)
            {
                float slotX = slotStartX + i * (slotW + 10f);
                if (bbShapePlaced[i])
                {
                    GUI.backgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.4f);
                    GUI.Box(new Rect(slotX, slotY, slotW, slotH), "");
                    continue;
                }

                GUI.backgroundColor = bbDragging && bbDragPiece == i ? new Color(0.3f, 0.3f, 0.35f, 0.5f) : new Color(0.22f, 0.22f, 0.28f, 0.9f);
                GUI.Box(new Rect(slotX, slotY, slotW, slotH), "");

                Rect slotRect = new Rect(slotX, slotY, slotW, slotH);
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && slotRect.Contains(Event.current.mousePosition) && bbGameActive)
                {
                    bbDragging = true;
                    bbDragPiece = i;
                    Event.current.Use();
                }

                Vector2Int[] s = bbShapeDefs[bbShapeTypes[i]];
                int maxR = 0, maxC = 0;
                foreach (var p in s) { if (p.y > maxR) maxR = p.y; if (p.x > maxC) maxC = p.x; }
                float scSize = 18f;
                float offX = slotX + (slotW - (maxC + 1) * scSize) / 2f;
                float offY = slotY + (slotH - (maxR + 1) * scSize) / 2f;
                Color shapeCol = bbBlockColors[bbShapeColors[i] % bbBlockColors.Length];
                if (bbDragging && bbDragPiece == i) shapeCol.a = 0.4f;
                foreach (var p in s)
                {
                    GUI.backgroundColor = shapeCol;
                    GUI.Button(new Rect(offX + p.x * scSize, offY + p.y * scSize, scSize - 1f, scSize - 1f), "");
                }
            }

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(gridX, slotY + slotH + 8f, cellSize * 8, 20f), "<size=10>Drag blocks onto the grid to place them</size>");

            if (bbComboTextTime > 0f && Time.time - bbComboTextTime < 1.5f)
            {
                GUI.color = Color.yellow;
                GUI.backgroundColor = Color.clear;
                GUI.Label(new Rect(gridX, gridY + cellSize * 3.5f, cellSize * 8, 30f), $"<size=18><b>{bbComboText}</b></size>");
                GUI.color = Color.white;
            }

            if (bbPopupTime > 0f && Time.time - bbPopupTime < 1f)
            {
                float pt = (Time.time - bbPopupTime) / 1f;
                GUI.color = new Color(1f, 1f, 0.3f, 1f - pt);
                GUI.backgroundColor = Color.clear;
                GUI.Label(new Rect(bbPopupX, bbPopupY - pt * 30f, 100f, 30f), $"+{bbPopupScore}");
                GUI.color = Color.white;
            }

            GUI.backgroundColor = guiColorB;
            if (!bbGameActive)
            {
                float goY = gridY + cellSize * 3f;
                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.75f);
                GUI.Box(new Rect(gridX - 5f, goY - 5f, cellSize * 8 + 10f, 130f), "");
                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;
                GUI.Label(new Rect(gridX, goY, cellSize * 8, 25f), "<size=14><b>Looks like you lost!</b></size>");
                GUI.Label(new Rect(gridX, goY + 22f, cellSize * 8, 25f), "<size=13>Want to try again?</size>");

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(gridX + cellSize * 2f, goY + 55f, 100f, 28f), "Yes"))
                {
                    BBNewGame();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                if (GUI.Button(new Rect(gridX + cellSize * 2f + 110f, goY + 55f, 100f, 28f), "No"))
                {
                    bbGameActive = false;
                    bbScore = 0;
                    bbGameOver = false;
                }
                GUI.color = Color.white;
            }

            GUI.EndScrollView();
        }

        private void BBNewGame()
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    bbGrid[r, c] = 0;
            bbScore = 0;
            bbSelectedShape = -1;
            bbGameActive = true;
            bbGameOver = false;
            bbComboTextTime = 0f;
            bbPopupTime = 0f;
            bbClearAnimTime = 0f;
            BBGenerateShapes();
        }

        private void BBGenerateShapes()
        {
            for (int i = 0; i < 3; i++)
            {
                bbShapeTypes[i] = UnityEngine.Random.Range(0, bbShapeDefs.Length);
                bbShapeColors[i] = UnityEngine.Random.Range(0, bbBlockColors.Length);
                bbShapePlaced[i] = false;
            }
            bbSelectedShape = -1;
        }

        private bool BBCanPlace(int shapeIdx, int row, int col)
        {
            Vector2Int[] shape = bbShapeDefs[shapeIdx];
            for (int i = 0; i < shape.Length; i++)
            {
                int r = row + shape[i].y;
                int c = col + shape[i].x;
                if (r < 0 || r >= 8 || c < 0 || c >= 8) return false;
                if (bbGrid[r, c] != 0) return false;
            }
            return true;
        }

        private void BBPlaceBlock(int pieceIdx, int row, int col)
        {
            Vector2Int[] shape = bbShapeDefs[bbShapeTypes[pieceIdx]];
            int colorVal = bbShapeColors[pieceIdx] + 1;
            for (int i = 0; i < shape.Length; i++)
            {
                int r = row + shape[i].y;
                int c = col + shape[i].x;
                bbGrid[r, c] = colorVal;
            }
            bbShapePlaced[pieceIdx] = true;
            bbSelectedShape = -1;
            BBClearLines();

            if (BBAllPlaced())
                BBGenerateShapes();

            if (!BBCanPlaceAny())
            {
                bbGameActive = false;
                bbGameOver = true;
                if (bbScore > bbBestScore) bbBestScore = bbScore;
            }
        }

        private bool BBAllPlaced()
        {
            for (int i = 0; i < 3; i++)
                if (!bbShapePlaced[i]) return false;
            return true;
        }

        private bool BBCanPlaceAny()
        {
            for (int i = 0; i < 3; i++)
            {
                if (bbShapePlaced[i]) continue;
                for (int r = 0; r < 8; r++)
                    for (int c = 0; c < 8; c++)
                        if (BBCanPlace(bbShapeTypes[i], r, c)) return true;
            }
            return false;
        }

        private void BBClearLines()
        {
            HashSet<string> toClear = new HashSet<string>();
            int linesCleared = 0;

            for (int r = 0; r < 8; r++)
            {
                bool full = true;
                for (int c = 0; c < 8; c++)
                    if (bbGrid[r, c] == 0) { full = false; break; }
                if (full)
                {
                    for (int c = 0; c < 8; c++)
                        toClear.Add(r + "," + c);
                    linesCleared++;
                }
            }

            for (int c = 0; c < 8; c++)
            {
                bool full = true;
                for (int r = 0; r < 8; r++)
                    if (bbGrid[r, c] == 0) { full = false; break; }
                if (full)
                {
                    for (int r = 0; r < 8; r++)
                        toClear.Add(r + "," + c);
                    linesCleared++;
                }
            }

            if (linesCleared > 0)
            {
                foreach (string key in toClear)
                {
                    string[] parts = key.Split(',');
                    bbGrid[int.Parse(parts[0]), int.Parse(parts[1])] = 0;
                }

                int pts = linesCleared * linesCleared * 10;
                bbScore += pts;

                float cellSize = 35f;
                bbPopupTime = Time.time;
                bbPopupScore = pts;
                bbPopupX = 20f + cellSize * 3f;
                bbPopupY = 10f + cellSize * 3f;

                bbComboTextTime = Time.time;
                if (linesCleared >= 4) bbComboText = "Quadra!!";
                else if (linesCleared >= 3) bbComboText = "Triple!!";
                else if (linesCleared >= 2) bbComboText = "Double!!";
                else bbComboText = "";

                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }

            bbClearAnimTime = linesCleared > 0 ? Time.time : 0f;
        }

        private void DrawSnake()
        {
            if (snakeGrid == null || !snakeGameActive && snakeScore == 0)
                SnakeNewGame();

            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Snake</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {snakeScore}  Best: {snakeBestScore}  Length: {snakeBody.Count}");

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(450f, 46f, 80f, 22f), "New Game"))
            {
                SnakeNewGame();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            string aiLabel = snakeUseAI ? "AI: ON" : "AI: OFF";
            GUI.backgroundColor = snakeUseAI ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
            if (GUI.Button(new Rect(540f, 46f, 60f, 22f), aiLabel))
            {
                snakeUseAI = !snakeUseAI;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            snakeScrollPos = GUI.BeginScrollView(
                new Rect(170f, 68f, guiRect.width - 170f, guiRect.height - 56f),
                snakeScrollPos,
                new Rect(0f, 0f, 500f, 560f),
                false, true);

            float cellSize = 20f;
            float gridX = 60f;
            float gridY = 10f;

            if (snakeAlive && snakeGameActive)
            {
                if (snakeUseAI)
                {
                    SnakeAIStep();
                }
                else
                {
                    if (Event.current.type == EventType.KeyDown)
                    {
                        KeyCode kc = Event.current.keyCode;
                        if (kc == KeyCode.UpArrow || kc == KeyCode.W)
                        { if (snakeDir != new Vector2Int(0, 1)) snakeDir = new Vector2Int(0, -1); }
                        else if (kc == KeyCode.DownArrow || kc == KeyCode.S)
                        { if (snakeDir != new Vector2Int(0, -1)) snakeDir = new Vector2Int(0, 1); }
                        else if (kc == KeyCode.LeftArrow || kc == KeyCode.A)
                        { if (snakeDir != new Vector2Int(1, 0)) snakeDir = new Vector2Int(-1, 0); }
                        else if (kc == KeyCode.RightArrow || kc == KeyCode.D)
                        { if (snakeDir != new Vector2Int(-1, 0)) snakeDir = new Vector2Int(1, 0); }
                    }
                }

                snakeMoveTimer += Time.deltaTime;
                if (snakeMoveTimer >= snakeMoveInterval)
                {
                    snakeMoveTimer -= snakeMoveInterval;
                    SnakeStep();
                }
            }

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
            GUI.Box(new Rect(gridX - 3, gridY - 3, cellSize * SnakeGridW + 6, cellSize * SnakeGridH + 6), "");

            for (int r = 0; r < SnakeGridH; r++)
            {
                for (int c = 0; c < SnakeGridW; c++)
                {
                    float cx = gridX + c * cellSize;
                    float cy = gridY + r * cellSize;
                    GUI.backgroundColor = new Color(0.16f, 0.16f, 0.19f, 0.9f);
                    GUI.Button(new Rect(cx + 0.3f, cy + 0.3f, cellSize - 0.6f, cellSize - 0.6f), "");
                }
            }

            GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
            float fx = gridX + snakeFood.x * cellSize;
            float fy = gridY + snakeFood.y * cellSize;
            GUI.Button(new Rect(fx + 1f, fy + 1f, cellSize - 2f, cellSize - 2f), "");

            for (int i = snakeBody.Count - 1; i >= 0; i--)
            {
                float t = (float)i / Mathf.Max(1, snakeBody.Count - 1);
                if (i == 0)
                    GUI.backgroundColor = new Color(0.2f, 0.9f, 0.3f);
                else
                    GUI.backgroundColor = Color.Lerp(new Color(0.1f, 0.7f, 0.2f), new Color(0.05f, 0.4f, 0.1f), t);

                float sx = gridX + snakeBody[i].x * cellSize;
                float sy = gridY + snakeBody[i].y * cellSize;
                GUI.Button(new Rect(sx + 0.5f, sy + 0.5f, cellSize - 1f, cellSize - 1f), "");
            }

            GUI.backgroundColor = guiColorB;
            string controls = snakeUseAI ? "AI is playing!" : "Arrow Keys / WASD to move";
            GUI.Label(new Rect(gridX, gridY + SnakeGridH * cellSize + 8f, cellSize * SnakeGridW, 20f), $"<size=11>{controls}</size>");

            float howY = gridY + SnakeGridH * cellSize + 30f;
            GUI.backgroundColor = new Color(0.14f, 0.14f, 0.18f, 0.9f);
            GUI.Box(new Rect(gridX - 5f, howY, cellSize * SnakeGridW + 10f, 90f), "");
            GUI.backgroundColor = Color.clear;
            GUI.color = guiColorA;
            GUI.Label(new Rect(gridX, howY + 4f, cellSize * SnakeGridW, 18f), "<size=13><b>How to Play</b></size>");
            GUI.color = Color.white;
            GUI.Label(new Rect(gridX + 5f, howY + 22f, cellSize * SnakeGridW, 16f), "<size=11>Use Arrow Keys or WASD to guide the snake</size>");
            GUI.Label(new Rect(gridX + 5f, howY + 38f, cellSize * SnakeGridW, 16f), "<size=11>Eat the red food to grow and earn +10 points</size>");
            GUI.Label(new Rect(gridX + 5f, howY + 54f, cellSize * SnakeGridW, 16f), "<size=11>Don't hit the walls or your own tail!</size>");
            GUI.Label(new Rect(gridX + 5f, howY + 70f, cellSize * SnakeGridW, 16f), "<size=11>Toggle AI to let the computer play for you</size>");

            if (!snakeAlive && snakeGameActive)
            {
                float goY = gridY + SnakeGridH * cellSize + 32f;
                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
                GUI.Box(new Rect(gridX - 5f, goY - 5f, cellSize * SnakeGridW + 10f, 100f), "");
                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;
                GUI.Label(new Rect(gridX, goY, cellSize * SnakeGridW, 25f), "<size=14><b>Game Over!</b></size>");
                GUI.Label(new Rect(gridX, goY + 22f, cellSize * SnakeGridW, 20f), $"<size=13>Score: {snakeScore}  Length: {snakeBody.Count}</size>");

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(gridX + cellSize * 4f, goY + 55f, 100f, 28f), "Play Again"))
                {
                    SnakeNewGame();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                if (GUI.Button(new Rect(gridX + cellSize * 4f + 110f, goY + 55f, 100f, 28f), "Quit"))
                {
                    snakeGameActive = false;
                    snakeScore = 0;
                }
                GUI.color = Color.white;
            }

            GUI.EndScrollView();
        }

        private void SnakeNewGame()
        {
            snakeGrid = new int[SnakeGridW, SnakeGridH];
            snakeBody = new List<Vector2Int>();
            int cx = SnakeGridW / 2;
            int cy = SnakeGridH / 2;
            snakeBody.Add(new Vector2Int(cx, cy));
            snakeBody.Add(new Vector2Int(cx - 1, cy));
            snakeBody.Add(new Vector2Int(cx - 2, cy));
            snakeDir = new Vector2Int(1, 0);
            snakeScore = 0;
            snakeAlive = true;
            snakeGameActive = true;
            snakeMoveTimer = 0f;
            snakeMoveInterval = 0.25f;
            snakePath = null;
            SnakeSpawnFood();
        }

        private void SnakeSpawnFood()
        {
            List<Vector2Int> empty = new List<Vector2Int>();
            for (int r = 0; r < SnakeGridH; r++)
                for (int c = 0; c < SnakeGridW; c++)
                    if (snakeGrid[c, r] == 0) empty.Add(new Vector2Int(c, r));
            if (empty.Count > 0)
                snakeFood = empty[UnityEngine.Random.Range(0, empty.Count)];
        }

        private void SnakeStep()
        {
            Vector2Int head = snakeBody[0];
            Vector2Int next = head + snakeDir;

            if (next.x < 0 || next.x >= SnakeGridW || next.y < 0 || next.y >= SnakeGridH)
            {
                snakeAlive = false;
                if (snakeScore > snakeBestScore) snakeBestScore = snakeScore;
                return;
            }

            for (int i = 0; i < snakeBody.Count; i++)
            {
                if (snakeBody[i] == next)
                {
                    snakeAlive = false;
                    if (snakeScore > snakeBestScore) snakeBestScore = snakeScore;
                    return;
                }
            }

            snakeBody.Insert(0, next);

            if (next == snakeFood)
            {
                snakeScore += 10;
                if (snakeMoveInterval > 0.08f)
                    snakeMoveInterval = Mathf.Max(0.08f, snakeMoveInterval - 0.001f);
                SnakeSpawnFood();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            else
            {
                snakeBody.RemoveAt(snakeBody.Count - 1);
            }
        }

        private void SnakeAIStep()
        {
            Vector2Int head = snakeBody[0];
            Vector2Int toFood = snakeFood - head;

            Vector2Int[] dirs = { new Vector2Int(0, -1), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(1, 0) };
            Vector2Int bestDir = snakeDir;
            float bestScore = -9999f;

            foreach (Vector2Int d in dirs)
            {
                if (d == -snakeDir) continue;
                Vector2Int next = head + d;
                if (next.x < 0 || next.x >= SnakeGridW || next.y < 0 || next.y >= SnakeGridH) continue;
                bool hitSelf = false;
                for (int i = 0; i < snakeBody.Count - 1; i++)
                {
                    if (snakeBody[i] == next) { hitSelf = true; break; }
                }
                if (hitSelf) continue;

                float score = 0f;
                if (d.x == toFood.x && toFood.x != 0) score += 2f;
                if (d.y == toFood.y && toFood.y != 0) score += 2f;

                int adjFree = 0;
                foreach (Vector2Int nd in dirs)
                {
                    Vector2Int nn = next + nd;
                    if (nn.x >= 0 && nn.x < SnakeGridW && nn.y >= 0 && nn.y < SnakeGridH)
                    {
                        bool blocked = false;
                        for (int i = 0; i < snakeBody.Count - 1; i++)
                        {
                            if (snakeBody[i] == nn) { blocked = true; break; }
                        }
                        if (!blocked) adjFree++;
                    }
                }
                score += adjFree * 0.5f;

                if (adjFree == 0) score -= 10f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = d;
                }
            }

            if (bestDir != -snakeDir || snakeBody.Count <= 3)
                snakeDir = bestDir;
        }

        private void DrawConnectFour()
        {
            if (c4Grid == null)
                C4NewGame();

            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Connect Four</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"You: 1  |  AI: 2  |  Wins: {c4ScoreX}  Losses: {c4ScoreO}");

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(450f, 46f, 80f, 22f), "New Game"))
            {
                C4NewGame();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            string[] diffNames = { "Easy", "Normal", "Hard" };
            GUI.Label(new Rect(170f, 68f, 60f, 20f), "Difficulty:");
            for (int d = 0; d < 3; d++)
            {
                GUI.backgroundColor = c4Diff == d ? guiColorA : guiColorB;
                if (GUI.Button(new Rect(240f + d * 65f, 68f, 58f, 20f), diffNames[d]))
                { c4Diff = d; C4NewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            }
            GUI.backgroundColor = guiColorB;

            c4ScrollPos = GUI.BeginScrollView(
                new Rect(170f, 90f, guiRect.width - 170f, guiRect.height - 78f),
                c4ScrollPos,
                new Rect(0f, 0f, 500f, 480f),
                false, true);

            float cellSize = 50f;
            float gap = 6f;
            float gridX = 60f;
            float gridY = 10f;

            GUI.backgroundColor = new Color(0.1f, 0.1f, 0.25f);
            GUI.Box(new Rect(gridX - 5f, gridY - 5f, C4Cols * (cellSize + gap) + 10f, C4Rows * (cellSize + gap) + 10f), "");

            for (int r = 0; r < C4Rows; r++)
            {
                for (int c = 0; c < C4Cols; c++)
                {
                    float cx = gridX + c * (cellSize + gap);
                    float cy = gridY + r * (cellSize + gap);
                    int val = c4Grid[c, r];

                    GUI.backgroundColor = new Color(0.06f, 0.06f, 0.1f);
                    GUI.Button(new Rect(cx + 0.5f, cy + 0.5f, cellSize - 1f, cellSize - 1f), "");

                    if (val == 1)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                        GUI.Button(new Rect(cx + 5f, cy + 5f, cellSize - 10f, cellSize - 10f), "");
                        GUI.backgroundColor = new Color(0.1f, 0.35f, 0.7f);
                        GUI.Button(new Rect(cx + 14f, cy + 14f, cellSize - 28f, cellSize - 28f), "");
                    }
                    else if (val == 2)
                    {
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.35f);
                        GUI.Button(new Rect(cx + 5f, cy + 5f, cellSize - 10f, cellSize - 10f), "");
                        GUI.backgroundColor = new Color(0.7f, 0.15f, 0.1f);
                        GUI.Button(new Rect(cx + 14f, cy + 14f, cellSize - 28f, cellSize - 28f), "");
                    }
                }
            }

            if (c4Winner == 0 && c4Turn == 0 && Time.time > c4AICooldown)
            {
                for (int c = 0; c < C4Cols; c++)
                {
                    float cx = gridX + c * (cellSize + gap);
                    float cy = gridY - cellSize - 5f;
                    GUI.backgroundColor = guiColorA;
                    if (GUI.Button(new Rect(cx + 0.5f, cy + 0.5f, cellSize - 1f, cellSize - 1f), "^"))
                    {
                        if (C4Drop(c, 1))
                        {
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            int w = C4CheckWin();
                            if (w == 1) { c4Winner = 1; c4ScoreX++; }
                            else if (w == 2) { c4Winner = 2; c4ScoreO++; }
                            else if (C4IsFull()) { c4Winner = 3; }
                            else
                            {
                                c4Turn = 1;
                                c4AICooldown = Time.time + 0.5f;
                                Invoke(nameof(C4AIMove), 0.45f);
                            }
                        }
                    }
                }
            }

            GUI.backgroundColor = guiColorB;

            if (c4Winner != 0)
            {
                float goY = gridY + C4Rows * (cellSize + gap) + 15f;
                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.7f);
                GUI.Box(new Rect(gridX - 5f, goY - 5f, C4Cols * (cellSize + gap) + 10f, 90f), "");
                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;

                string msg = c4Winner == 1 ? "You Win!" : c4Winner == 2 ? "AI Wins!" : "Draw!";
                GUI.Label(new Rect(gridX, goY, C4Cols * (cellSize + gap), 25f), $"<size=16><b>{msg}</b></size>");

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(gridX + cellSize * 1.5f, goY + 35f, 100f, 28f), "Play Again"))
                {
                    C4NewGame();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                if (GUI.Button(new Rect(gridX + cellSize * 1.5f + 110f, goY + 35f, 100f, 28f), "Quit"))
                {
                    c4Winner = 4;
                    c4Grid = null;
                }
                GUI.color = Color.white;
            }

            float howY = gridY + C4Rows * (cellSize + gap) + 15f;
            if (c4Winner != 0) howY += 100f;
            GUI.backgroundColor = new Color(0.14f, 0.14f, 0.18f, 0.9f);
            GUI.Box(new Rect(gridX - 5f, howY, C4Cols * (cellSize + gap) + 10f, 90f), "");
            GUI.backgroundColor = Color.clear;
            GUI.color = guiColorA;
            GUI.Label(new Rect(gridX, howY + 4f, C4Cols * (cellSize + gap), 18f), "<size=13><b>How to Play</b></size>");
            GUI.color = Color.white;
            GUI.Label(new Rect(gridX + 5f, howY + 22f, C4Cols * (cellSize + gap), 16f), "<size=11>Click the ^ buttons above a column to drop your piece</size>");
            GUI.Label(new Rect(gridX + 5f, howY + 38f, C4Cols * (cellSize + gap), 16f), "<size=11>First to connect 4 pieces in a row wins</size>");
            GUI.Label(new Rect(gridX + 5f, howY + 54f, C4Cols * (cellSize + gap), 16f), "<size=11>Rows, columns, and diagonals all count</size>");
            GUI.Label(new Rect(gridX + 5f, howY + 70f, C4Cols * (cellSize + gap), 16f), "<size=11>Blue = You  |  Red = AI  |  Adjust difficulty above</size>");

            GUI.EndScrollView();
        }

        private void C4NewGame()
        {
            c4Grid = new int[C4Cols, C4Rows];
            c4Winner = 0;
            c4Turn = 0;
            c4AICooldown = 0f;
        }

        private bool C4Drop(int col, int player)
        {
            for (int r = C4Rows - 1; r >= 0; r--)
            {
                if (c4Grid[col, r] == 0)
                {
                    c4Grid[col, r] = player;
                    return true;
                }
            }
            return false;
        }

        private bool C4IsFull()
        {
            for (int c = 0; c < C4Cols; c++)
                if (c4Grid[c, 0] == 0) return false;
            return true;
        }

        private int C4CheckWin()
        {
            for (int c = 0; c < C4Cols; c++)
            {
                for (int r = 0; r < C4Rows; r++)
                {
                    int v = c4Grid[c, r];
                    if (v == 0) continue;
                    int[][] dirs = { new[]{1,0}, new[]{0,1}, new[]{1,1}, new[]{1,-1} };
                    foreach (var d in dirs)
                    {
                        bool win = true;
                        for (int i = 1; i < 4; i++)
                        {
                            int nc = c + d[0] * i;
                            int nr = r + d[1] * i;
                            if (nc < 0 || nc >= C4Cols || nr < 0 || nr >= C4Rows || c4Grid[nc, nr] != v)
                            { win = false; break; }
                        }
                        if (win) return v;
                    }
                }
            }
            return 0;
        }

        private int C4Eval()
        {
            int w = C4CheckWin();
            if (w == 2) return 1000;
            if (w == 1) return -1000;
            int score = 0;
            for (int c = 0; c < C4Cols; c++)
            {
                if (c4Grid[c, 0] == 0) score += (c == 3 ? 3 : (c == 2 || c == 4) ? 2 : 1);
            }
            return score;
        }

        private void C4AIMove()
        {
            if (c4Winner != 0 || c4Turn != 1) return;

            int bestCol = 3;
            if (c4Diff == 0)
            {
                List<int> avail = new List<int>();
                for (int c = 0; c < C4Cols; c++)
                    if (c4Grid[c, 0] == 0) avail.Add(c);
                bestCol = avail[UnityEngine.Random.Range(0, avail.Count)];
            }
            else if (c4Diff == 1)
            {
                bestCol = C4MinimaxRoot(3);
                if (UnityEngine.Random.value < 0.3f)
                {
                    List<int> avail = new List<int>();
                    for (int c = 0; c < C4Cols; c++)
                        if (c4Grid[c, 0] == 0) avail.Add(c);
                    bestCol = avail[UnityEngine.Random.Range(0, avail.Count)];
                }
            }
            else
            {
                bestCol = C4MinimaxRoot(5);
            }

            C4Drop(bestCol, 2);
            int win = C4CheckWin();
            if (win == 2) { c4Winner = 2; c4ScoreO++; }
            else if (win == 1) { c4Winner = 1; c4ScoreX++; }
            else if (C4IsFull()) { c4Winner = 3; }
            else { c4Turn = 0; }
        }

        private int C4MinimaxRoot(int depth)
        {
            int bestScore = -99999;
            int bestCol = 3;
            List<int> order = new List<int> { 3, 2, 4, 1, 5, 0, 6 };

            foreach (int c in order)
            {
                if (c4Grid[c, 0] != 0) continue;
                C4Drop(c, 2);
                int score = C4MinimaxHelper(depth - 1, false, -99999, 99999);

                for (int r = 0; r < C4Rows; r++)
                {
                    if (c4Grid[c, r] != 0)
                    {
                        c4Grid[c, r] = 0;
                        break;
                    }
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCol = c;
                }
            }
            return bestCol;
        }

        private int C4MinimaxHelper(int depth, bool maximizing, int alpha, int beta)
        {
            int w = C4CheckWin();
            if (w == 2) return 1000 + depth;
            if (w == 1) return -1000 - depth;
            if (depth == 0 || C4IsFull()) return C4Eval();

            if (maximizing)
            {
                int best = -99999;
                for (int c = 0; c < C4Cols; c++)
                {
                    if (c4Grid[c, 0] != 0) continue;
                    C4Drop(c, 2);
                    int val = C4MinimaxHelper(depth - 1, false, alpha, beta);
                    int row = -1;
                    for (int r = 0; r < C4Rows; r++)
                        if (c4Grid[c, r] != 0) { row = r; break; }
                    if (row >= 0) c4Grid[c, row] = 0;
                    best = Math.Max(best, val);
                    alpha = Math.Max(alpha, val);
                    if (beta <= alpha) break;
                }
                return best;
            }
            else
            {
                int best = 99999;
                for (int c = 0; c < C4Cols; c++)
                {
                    if (c4Grid[c, 0] != 0) continue;
                    C4Drop(c, 1);
                    int val = C4MinimaxHelper(depth - 1, true, alpha, beta);
                    int row = -1;
                    for (int r = 0; r < C4Rows; r++)
                        if (c4Grid[c, r] != 0) { row = r; break; }
                    if (row >= 0) c4Grid[c, row] = 0;
                    best = Math.Min(best, val);
                    beta = Math.Min(beta, val);
                    if (beta <= alpha) break;
                }
                return best;
            }
        }

        private int C4FirstEmpty(int col)
        {
            for (int r = C4Rows - 1; r >= 0; r--)
                if (c4Grid[col, r] == 0) return r;
            return -1;
        }

        private void DrawFlappyBird()
        {
            if (fbPipeX == null)
                FBNewGame();

            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Flappy Bird</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {fbScore}  Best: {fbBestScore}");

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(450f, 46f, 80f, 22f), "New Game"))
            {
                FBNewGame();
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            fbScrollPos = GUI.BeginScrollView(
                new Rect(170f, 68f, guiRect.width - 170f, guiRect.height - 56f),
                fbScrollPos,
                new Rect(0f, 0f, 500f, 500f),
                false, true);

            float fieldX = 20f;
            float fieldY = 10f;
            float fieldW = 460f;
            float fieldH = 350f;
            float birdSize = 20f;
            float pipeW = 40f;
            float gapH = 90f;
            float groundH = 30f;

            if (fbAlive && fbGameActive)
            {
                if (Event.current.type == EventType.MouseDown || (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Space || Event.current.keyCode == KeyCode.UpArrow || Event.current.keyCode == KeyCode.W)))
                {
                    fbBirdVel = -260f;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    Event.current.Use();
                }

                float dt = Time.deltaTime;
                fbBirdVel += 420f * dt;
                fbBirdY += fbBirdVel * dt;
                fbGroundOffset = (fbGroundOffset + 100f * dt) % 20f;

                for (int i = fbPipeX.Count - 1; i >= 0; i--)
                {
                    fbPipeX[i] -= 120f * dt;
                    if (fbPipeX[i] + pipeW < fbBirdX)
                    {
                        fbPipeX.RemoveAt(i);
                        fbPipeGap.RemoveAt(i);
                    }
                }

                if (fbPipeX.Count == 0 || fbPipeX[fbPipeX.Count - 1] < fieldX + fieldW - 160f)
                {
                    float minGap = fieldY + 40f;
                    float maxGap = fieldY + fieldH - groundH - gapH - 40f;
                    float gapCenter = UnityEngine.Random.Range(minGap, maxGap);
                    fbPipeX.Add(fieldX + fieldW);
                    fbPipeGap.Add(gapCenter);
                }

                for (int i = 0; i < fbPipeX.Count; i++)
                {
                    float px = fbPipeX[i];
                    float gc = fbPipeGap[i];
                    if (px + pipeW > fbBirdX && px < fbBirdX + birdSize)
                    {
                        if (fbBirdY < gc || fbBirdY + birdSize > gc + gapH)
                        {
                            fbAlive = false;
                            if (fbScore > fbBestScore) fbBestScore = fbScore;
                        }
                    }
                    if (px + pipeW > fbBirdX + birdSize && px + pipeW < fbBirdX + birdSize + 120f * dt)
                    {
                        fbScore++;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }

                if (fbBirdY < fieldY || fbBirdY + birdSize > fieldY + fieldH - groundH)
                {
                    fbAlive = false;
                    if (fbScore > fbBestScore) fbBestScore = fbScore;
                }
            }

            GUI.backgroundColor = new Color(0.53f, 0.81f, 0.92f);
            GUI.Box(new Rect(fieldX, fieldY, fieldW, fieldH - groundH), "");
            GUI.backgroundColor = guiColorB;

            for (int i = 0; i < fbPipeX.Count; i++)
            {
                float px = fbPipeX[i];
                float gc = fbPipeGap[i];
                if (px > fieldX + fieldW || px + pipeW < fieldX) continue;

                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                GUI.Button(new Rect(px, fieldY, pipeW, gc - fieldY), "");
                GUI.Button(new Rect(px, gc + gapH, pipeW, fieldY + fieldH - groundH - gc - gapH), "");

                GUI.backgroundColor = new Color(0.15f, 0.55f, 0.25f);
                GUI.Button(new Rect(px + 3f, fieldY, 6f, gc - fieldY), "");
                GUI.Button(new Rect(px + 3f, gc + gapH, 6f, fieldY + fieldH - groundH - gc - gapH), "");
            }

            GUI.backgroundColor = new Color(1f, 0.85f, 0.2f);
            float birdDrawY = Mathf.Clamp(fbBirdY, fieldY, fieldY + fieldH - groundH - birdSize);
            GUI.Button(new Rect(fbBirdX, birdDrawY, birdSize, birdSize), "");

            GUI.backgroundColor = new Color(1f, 0.6f, 0.1f);
            GUI.Button(new Rect(fbBirdX + birdSize - 4f, birdDrawY + 5f, 8f, 6f), "");

            GUI.backgroundColor = Color.clear;
            GUI.color = Color.white;
            GUIStyle fbScoreStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
            GUI.Label(new Rect(fieldX, fieldY + 5f, fieldW, 30f), fbScore.ToString(), fbScoreStyle);
            GUI.color = Color.white;

            GUI.backgroundColor = new Color(0.85f, 0.75f, 0.4f);
            GUI.Box(new Rect(fieldX, fieldY + fieldH - groundH, fieldW, groundH), "");
            for (float gx = -fbGroundOffset; gx < fieldW; gx += 20f)
            {
                GUI.backgroundColor = new Color(0.7f, 0.6f, 0.3f);
                GUI.Box(new Rect(fieldX + gx, fieldY + fieldH - groundH, 10f, groundH), "");
            }

            if (!fbAlive && fbGameActive)
            {
                GUI.backgroundColor = new Color(0f, 0f, 0f, 0.6f);
                GUI.Box(new Rect(fieldX, fieldY, fieldW, fieldH), "");
                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;
                GUIStyle goStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(fieldX, fieldY + fieldH * 0.3f, fieldW, 30f), "Game Over!", goStyle);
                GUIStyle subGoStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(fieldX, fieldY + fieldH * 0.3f + 28f, fieldW, 20f), $"Score: {fbScore}  Best: {fbBestScore}", subGoStyle);

                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(fieldX + fieldW / 2f - 60f, fieldY + fieldH * 0.3f + 60f, 120f, 30f), "Play Again"))
                {
                    FBNewGame();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.backgroundColor = new Color(0.4f, 0.4f, 0.4f);
                if (GUI.Button(new Rect(fieldX + fieldW / 2f - 60f, fieldY + fieldH * 0.3f + 95f, 120f, 30f), "Quit"))
                {
                    fbGameActive = false;
                    fbScore = 0;
                }
                GUI.color = Color.white;
            }

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(fieldX, fieldY + fieldH + 5f, fieldW, 16f), "<size=11>Click, Space, or W/Up to flap</size>");

            float howY2 = fieldY + fieldH + 25f;
            GUI.backgroundColor = new Color(0.14f, 0.14f, 0.18f, 0.9f);
            GUI.Box(new Rect(fieldX - 5f, howY2, fieldW + 10f, 80f), "");
            GUI.backgroundColor = Color.clear;
            GUI.color = guiColorA;
            GUI.Label(new Rect(fieldX, howY2 + 4f, fieldW, 18f), "<size=13><b>How to Play</b></size>");
            GUI.color = Color.white;
            GUI.Label(new Rect(fieldX + 5f, howY2 + 22f, fieldW, 16f), "<size=11>Click, Space, or W/Up arrow to flap and gain altitude</size>");
            GUI.Label(new Rect(fieldX + 5f, howY2 + 38f, fieldW, 16f), "<size=11>Navigate through the gaps between green pipes</size>");
            GUI.Label(new Rect(fieldX + 5f, howY2 + 54f, fieldW, 16f), "<size=11>Score increases for each pipe you pass. Don't hit the ground!</size>");

            GUI.EndScrollView();
        }

        private void FBNewGame()
        {
            float fieldH = 350f;
            float groundH = 30f;
            fbBirdY = (fieldH - groundH) / 2f - 10f;
            fbBirdVel = 0f;
            fbBirdX = 80f;
            fbPipeX = new List<float>();
            fbPipeGap = new List<float>();
            fbScore = 0;
            fbAlive = true;
            fbGameActive = true;
            fbGroundOffset = 0f;
        }

        private void DrawMinesweeper()
        {
            if (msGrid == null) MSNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Minesweeper</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Mines: {msMines}  Flags Left: {msFlagsLeft}  Time: {msTimer:F1}s");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 90f, 22f), msFlagMode ? "⛏ Dig" : "🚩 Flag"))
            { msFlagMode = !msFlagMode; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(530f, 46f, 80f, 22f), "New Game"))
            { MSNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            if (!msGameOver && msStarted) msTimer += Time.deltaTime;
            float cellSize = 26f;
            float ox = 180f, oy = 72f;
            for (int r = 0; r < msRows; r++)
            {
                for (int c = 0; c < msCols; c++)
                {
                    float x = ox + c * cellSize, y = oy + r * cellSize;
                    if (msRevealed[r, c])
                    {
                        if (msGrid[r, c] == -1)
                        {
                            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                            GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), "💣");
                        }
                        else
                        {
                            GUI.backgroundColor = new Color(0.75f, 0.75f, 0.8f);
                            int val = msGrid[r, c];
                            string txt = val > 0 ? val.ToString() : "";
                            GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), txt);
                        }
                    }
                    else if (msFlagged[r, c])
                    {
                        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
                        if (GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), "🚩"))
                        {
                            if (!msGameOver)
                            {
                                msFlagged[r, c] = false;
                                msFlagsLeft++;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.55f, 0.6f, 0.65f);
                        if (GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), ""))
                        {
                            if (!msGameOver)
                            {
                                if (msFlagMode)
                                {
                                    msFlagged[r, c] = true;
                                    msFlagsLeft--;
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                else
                                {
                                    MSReveal(r, c);
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                            }
                        }
                    }
                }
            }
            if (msGameOver)
            {
                GUI.backgroundColor = Color.clear;
                GUI.color = Color.white;
                GUIStyle msStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                string msg = msWon ? "You Win!" : "Game Over!";
                GUI.Label(new Rect(ox, oy + msRows * cellSize + 5f, msCols * cellSize, 25f), msg, msStyle);
                GUI.color = Color.white;
            }
            GUI.backgroundColor = guiColorB;
        }
        private void MSNewGame()
        {
            msGrid = new int[msRows, msCols];
            msRevealed = new bool[msRows, msCols];
            msFlagged = new bool[msRows, msCols];
            int placed = 0;
            while (placed < msMines)
            {
                int r = UnityEngine.Random.Range(0, msRows), c = UnityEngine.Random.Range(0, msCols);
                if (msGrid[r, c] != -1) { msGrid[r, c] = -1; placed++; }
            }
            for (int r = 0; r < msRows; r++)
                for (int c = 0; c < msCols; c++)
                {
                    if (msGrid[r, c] == -1) continue;
                    int cnt = 0;
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        {
                            int nr = r + dr, nc = c + dc;
                            if (nr >= 0 && nr < msRows && nc >= 0 && nc < msCols && msGrid[nr, nc] == -1) cnt++;
                        }
                    msGrid[r, c] = cnt;
                }
            msFlagsLeft = msMines;
            msGameOver = false; msWon = false; msStarted = false; msTimer = 0f; msFlagMode = false;
        }
        private void MSReveal(int r, int c)
        {
            if (r < 0 || r >= msRows || c < 0 || c >= msCols || msRevealed[r, c] || msFlagged[r, c]) return;
            msRevealed[r, c] = true;
            msStarted = true;
            if (msGrid[r, c] == -1)
            {
                msGameOver = true;
                for (int rr = 0; rr < msRows; rr++)
                    for (int cc = 0; cc < msCols; cc++)
                        if (msGrid[rr, cc] == -1) msRevealed[rr, cc] = true;
                return;
            }
            if (msGrid[r, c] == 0)
            {
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                        MSReveal(r + dr, c + dc);
            }
            int unrevealed = 0;
            for (int rr = 0; rr < msRows; rr++)
                for (int cc = 0; cc < msCols; cc++)
                    if (!msRevealed[rr, cc] && msGrid[rr, cc] != -1) unrevealed++;
            if (unrevealed == 0) { msGameOver = true; msWon = true; }
        }

        private void Draw2048()
        {
            if (g4Grid == null) G4NewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>2048</size>");
            GUI.Label(new Rect(170f, 48f, 200f, 20f), $"Score: {g4Score}  Best: {g4Best}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { G4NewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && !e.shift && !e.control && !e.alt)
            {
                bool moved = false;
                if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) { moved = G4Slide(0); e.Use(); }
                else if (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) { moved = G4Slide(1); e.Use(); }
                else if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) { moved = G4Slide(2); e.Use(); }
                else if (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) { moved = G4Slide(3); e.Use(); }
                if (moved) { G4Spawn(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            }
            float cs = 60f, ox = 190f, oy = 72f;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    int v = g4Grid[r, c];
                    float x = ox + c * cs, y = oy + r * cs;
                    GUI.backgroundColor = G4TileColor(v);
                    GUI.Button(new Rect(x, y, cs - 2, cs - 2), v > 0 ? v.ToString() : "");
                }
            GUI.backgroundColor = guiColorB;
            if (g4Won)
            {
                GUI.color = Color.yellow;
                GUIStyle ws = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 4 * cs + 10f, 4 * cs, 30f), "You Win! Reached 2048!", ws);
                GUI.color = Color.white;
            }
            else if (!G4CanMove())
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle ws = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 4 * cs + 10f, 4 * cs, 30f), "No moves left!", ws);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + 4 * cs + 40f, 4 * cs, 20f), "<size=11>Arrow keys or WASD to slide tiles</size>");
        }
        private void G4NewGame()
        {
            g4Grid = new int[4, 4];
            g4Score = 0; g4Active = true; g4Won = false;
            G4Spawn(); G4Spawn();
        }
        private void G4Spawn()
        {
            List<int> empty = new List<int>();
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    if (g4Grid[r, c] == 0) empty.Add(r * 4 + c);
            if (empty.Count == 0) return;
            int idx = empty[UnityEngine.Random.Range(0, empty.Count)];
            g4Grid[idx / 4, idx % 4] = UnityEngine.Random.Range(0, 10) < 9 ? 2 : 4;
        }
        private bool G4Slide(int dir)
        {
            bool moved = false;
            int[] dr = { -1, 1, 0, 0 }, dc = { 0, 0, -1, 1 };
            for (int pass = 0; pass < 3; pass++)
            {
                for (int r = 0; r < 4; r++)
                    for (int c = 0; c < 4; c++)
                    {
                        int nr = r + dr[dir], nc = c + dc[dir];
                        if (nr < 0 || nr >= 4 || nc < 0 || nc >= 4) continue;
                        if (g4Grid[nr, nc] == 0 && g4Grid[r, c] != 0)
                        {
                            g4Grid[nr, nc] = g4Grid[r, c];
                            g4Grid[r, c] = 0;
                            moved = true;
                        }
                        else if (g4Grid[nr, nc] == g4Grid[r, c] && g4Grid[r, c] != 0)
                        {
                            g4Grid[nr, nc] *= 2;
                            g4Score += g4Grid[nr, nc];
                            if (g4Grid[nr, nc] == 2048) g4Won = true;
                            if (g4Score > g4Best) g4Best = g4Score;
                            g4Grid[r, c] = 0;
                            moved = true;
                        }
                    }
            }
            return moved;
        }
        private bool G4CanMove()
        {
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    if (g4Grid[r, c] == 0) return true;
                    if (c < 3 && g4Grid[r, c] == g4Grid[r, c + 1]) return true;
                    if (r < 3 && g4Grid[r, c] == g4Grid[r + 1, c]) return true;
                }
            return false;
        }
        private Color G4TileColor(int v)
        {
            if (v == 0) return new Color(0.18f, 0.18f, 0.22f);
            if (v == 2) return new Color(0.9f, 0.9f, 0.85f);
            if (v == 4) return new Color(0.9f, 0.88f, 0.7f);
            if (v == 8) return new Color(0.95f, 0.65f, 0.3f);
            if (v == 16) return new Color(0.95f, 0.5f, 0.25f);
            if (v == 32) return new Color(0.9f, 0.3f, 0.2f);
            if (v == 64) return new Color(0.85f, 0.15f, 0.15f);
            if (v == 128) return new Color(0.95f, 0.9f, 0.4f);
            if (v == 256) return new Color(0.95f, 0.88f, 0.35f);
            if (v == 512) return new Color(0.95f, 0.85f, 0.3f);
            if (v == 1024) return new Color(0.95f, 0.8f, 0.25f);
            return new Color(0.95f, 0.75f, 0.2f);
        }

        private void DrawPong()
        {
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Pong</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"You: {pongPScore}  AI: {pongEScore}  First to 10");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { PongReset(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            if (!pongStarted) PongReset();
            float ox = 190f, oy = 72f;
            float pw = 10f, ph = 50f;
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
                pongPlayerY = Mathf.Clamp(e.mousePosition.y - oy - ph / 2f, 0f, pongFieldH - ph);
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) pongPlayerY = Mathf.Max(0f, pongPlayerY - 15f);
                if (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) pongPlayerY = Mathf.Min(pongFieldH - ph, pongPlayerY + 15f);
            }
            float spd = 3.5f;
            float centerDist = (pongEnemyY + ph / 2f) - (pongBallY + 4f);
            if (centerDist > 5f) pongEnemyY -= spd;
            else if (centerDist < -5f) pongEnemyY += spd;
            pongEnemyY = Mathf.Clamp(pongEnemyY, 0f, pongFieldH - ph);
            pongBallX += pongBallVX * Time.deltaTime;
            pongBallY += pongBallVY * Time.deltaTime;
            if (pongBallY <= 0f || pongBallY + 8f >= pongFieldH)
            { pongBallVY = -pongBallVY; pongBallY = Mathf.Clamp(pongBallY, 0f, pongFieldH - 8f); }
            float pL = ox + 2f, pR = ox + pongFieldW - pw - 2f;
            Rect prRect = new Rect(pL, oy + pongPlayerY, pw, ph);
            Rect peRect = new Rect(pR, oy + pongEnemyY, pw, ph);
            Rect brRect = new Rect(ox + pongBallX, oy + pongBallY, 8f, 8f);
            if (brRect.Overlaps(prRect) && pongBallVX < 0f)
            { pongBallVX = -pongBallVX * 1.05f; float hit = (pongBallY + 4f - (pongPlayerY + ph / 2f)) / (ph / 2f); pongBallVY += hit * 200f; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (brRect.Overlaps(peRect) && pongBallVX > 0f)
            { pongBallVX = -pongBallVX * 1.05f; float hit = (pongBallY + 4f - (pongEnemyY + ph / 2f)) / (ph / 2f); pongBallVY += hit * 200f; }
            if (pongBallX < -10f) { pongEScore++; PongServe(1); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (pongBallX > pongFieldW + 10f) { pongPScore++; PongServe(-1); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (pongPScore >= 10 || pongEScore >= 10)
            {
                pongStarted = false;
                GUI.color = Color.yellow;
                GUIStyle ws = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                string msg = pongPScore >= 10 ? "You Win!" : "AI Wins!";
                GUI.Label(new Rect(ox, oy + pongFieldH / 2f - 15f, pongFieldW, 30f), msg, ws);
                GUI.color = Color.white;
            }
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
            GUI.Button(new Rect(ox, oy, pongFieldW, pongFieldH), "");
            GUI.backgroundColor = new Color(1f, 1f, 1f);
            GUI.Button(new Rect(ox + pongFieldW / 2f - 1f, oy, 2f, pongFieldH), "");
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Button(prRect, "");
            GUI.Button(peRect, "");
            GUI.backgroundColor = Color.yellow;
            GUI.Button(brRect, "");
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + pongFieldH + 5f, pongFieldW, 16f), "<size=11>Mouse or W/S to move paddle</size>");
        }
        private void PongReset()
        {
            pongPlayerY = pongFieldH / 2f - 25f;
            pongEnemyY = pongFieldH / 2f - 25f;
            pongPScore = 0; pongEScore = 0;
            pongStarted = true;
            PongServe(1);
        }
        private void PongServe(int dir)
        {
            pongBallX = pongFieldW / 2f - 4f;
            pongBallY = pongFieldH / 2f - 4f;
            pongBallVX = dir * 250f;
            pongBallVY = UnityEngine.Random.Range(-100f, 100f);
        }

        private void DrawSimon()
        {
            if (simonPattern == null) SimonNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Simon Says</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {simonScore}  High: {simonHigh}");
            GUI.backgroundColor = guiColorB;
            if (simonPhase == 2)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(240f, 100f, 200f, 30f), "Game Over! Watch the pattern.", gs);
                GUI.color = Color.white;
            }
            float bw = 100f, bh = 100f, ox = 200f, oy = 72f, gap = 10f;
            for (int i = 0; i < 4; i++)
            {
                float x = ox + (i % 2) * (bw + gap);
                float y = oy + (i / 2) * (bh + gap);
                Color col = simonCols[i];
                bool active = simonFlash && simonFlashI == i;
                GUI.backgroundColor = active ? Color.white : col;
                if (GUI.Button(new Rect(x, y, bw, bh), "") && simonPhase == 1)
                {
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    if (i == simonPattern[simonPI])
                    {
                        simonPI++;
                        if (simonPI >= simonPattern.Length)
                        {
                            simonScore++;
                            if (simonScore > simonHigh) simonHigh = simonScore;
                            SimonNextRound();
                        }
                    }
                    else
                    {
                        simonPhase = 2;
                        if (simonScore > simonHigh) simonHigh = simonScore;
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + 2 * (bh + gap) + 5f, 2 * bw + gap, 20f), "<size=11>Watch the sequence, then repeat it!</size>");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(ox + bw / 2f, oy + 2 * (bh + gap) + 28f, bw + gap, 22f), "Restart"))
            { SimonNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            if (simonPhase == 0)
            {
                simonTimer -= Time.deltaTime;
                if (simonTimer <= 0f)
                {
                    if (simonFlashCount < simonPattern.Length)
                    {
                        simonFlash = true;
                        simonFlashI = simonPattern[simonFlashCount];
                        simonFlashCount++;
                        simonTimer = 0.6f;
                    }
                    else
                    {
                        simonFlash = false;
                        simonPhase = 1;
                        simonPI = 0;
                    }
                }
                if (simonFlash && simonTimer < 0.3f) simonFlash = false;
            }
        }
        private void SimonNewGame()
        {
            simonPattern = new int[] { UnityEngine.Random.Range(0, 4) };
            simonScore = 0; simonPhase = 0; simonPI = 0;
            simonTimer = 1f; simonFlashCount = 0; simonFlash = false;
        }
        private void SimonNextRound()
        {
            int[] old = simonPattern;
            simonPattern = new int[old.Length + 1];
            for (int i = 0; i < old.Length; i++) simonPattern[i] = old[i];
            simonPattern[old.Length] = UnityEngine.Random.Range(0, 4);
            simonPhase = 0; simonPI = 0;
            simonTimer = 1f; simonFlashCount = 0;
        }

        private static readonly string[] hmWords = { "computer", "gorilla", "banana", "jungle", "forest", "moon", "rocket", "planet", "galaxy", "ocean", "mountain", "river", "castle", "dragon", "wizard", "puzzle", "guitar", "piano", "basket", "blanket", "dolphin", "eclipse", "feather", "harvest", "lantern", "magnet", "nebula", "parrot", "shadow", "tunnel", "wizard", "zephyr", "anchor", "blaze", "cactus", "dagger", "emerald", "falcon", "glacier", "horizon", "ivory", "jasper", "knight", "legend", "mirage", "nectar", "oracle", "phoenix", "quartz", "ripple" };
        private void DrawHangman()
        {
            if (string.IsNullOrEmpty(hmWord)) HMNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Hangman</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Wrong: {hmWrong}/6");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Word"))
            { HMNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            string display = "";
            foreach (char c in hmWord)
                display += (hmGuesses.IndexOf(c) >= 0) ? c + " " : "_ ";
            GUI.color = Color.white;
            GUIStyle ws = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(200f, 72f, 280f, 35f), display.Trim(), ws);
            GUI.color = Color.white;
            string[] bodyParts = { "💀", "👀", "👃", "✋", "🦶", "😫" };
            GUI.Label(new Rect(200f, 108f, 280f, 30f), hmWrong > 0 ? string.Join(" ", System.Linq.Enumerable.Take(bodyParts, hmWrong)) : "");
            float ox = 200f, oy = 145f;
            float bw = 28f, bh = 26f;
            string alphabet = "abcdefghijklmnopqrstuvwxyz";
            for (int i = 0; i < 26; i++)
            {
                int row = i / 13, col = i % 13;
                char ch = alphabet[i];
                bool guessed = hmGuesses.IndexOf(ch) >= 0;
                GUI.backgroundColor = guessed ? new Color(0.3f, 0.3f, 0.35f) : guiColorA;
                if (!guessed && !hmWon && !hmLost && GUI.Button(new Rect(ox + col * (bw + 2f), oy + row * (bh + 2f), bw, bh), ch.ToString()))
                {
                    hmGuesses += ch;
                    if (hmWord.IndexOf(ch) < 0) hmWrong++;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    if (hmWrong >= 6) hmLost = true;
                    bool allFound = true;
                    foreach (char c in hmWord)
                        if (hmGuesses.IndexOf(c) < 0) { allFound = false; break; }
                    if (allFound) hmWon = true;
                }
            }
            GUI.backgroundColor = guiColorB;
            if (hmWon)
            {
                GUI.color = Color.green;
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 70f, 13 * (bw + 2f), 25f), $"You win! The word was: {hmWord}", gs);
                GUI.color = Color.white;
            }
            else if (hmLost)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 70f, 13 * (bw + 2f), 25f), $"Game Over! Word: {hmWord}", gs);
                GUI.color = Color.white;
            }
        }
        private void HMNewGame()
        {
            hmWord = hmWords[UnityEngine.Random.Range(0, hmWords.Length)];
            hmGuesses = ""; hmWrong = 0; hmWon = false; hmLost = false;
        }

        private void DrawMemory()
        {
            if (mmGrid == null) MMNewGame();
            if (!mmTexturesLoaded) LoadMMTextures();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Memory Match</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Pairs: {mmPairs}/8  Moves: {mmMoves}  Time: {mmTimer:F1}s");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { MMNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            if (!mmDone) mmTimer += Time.deltaTime;
            float cs = 70f, ox = 200f, oy = 72f;
            if (mmBusy) mmFlipBack -= Time.deltaTime;
            if (mmFlipBack <= 0f && mmR2 >= 0)
            {
                if (mmGrid[mmR1, mmC1] != mmGrid[mmR2, mmC2])
                {
                    mmOpen[mmR1, mmC1] = false;
                    mmOpen[mmR2, mmC2] = false;
                }
                mmR1 = mmC1 = mmR2 = mmC2 = -1;
                mmBusy = false;
            }
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    Rect cardRect = new Rect(x, y, cs - 3, cs - 3);
                    if (mmMatched[r, c] || mmOpen[r, c])
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.7f, 0.3f);
                        GUI.Button(cardRect, "");
                        if (mmTextures != null && mmGrid[r, c] < mmTextures.Length && mmTextures[mmGrid[r, c]] != null)
                        {
                            GUI.DrawTexture(new Rect(x + 3, y + 3, cs - 9, cs - 9), mmTextures[mmGrid[r, c]], ScaleMode.StretchToFill);
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.4f, 0.5f, 0.7f);
                        if (GUI.Button(cardRect, "?"))
                        {
                            if (!mmBusy && !mmDone)
                            {
                                mmOpen[r, c] = true;
                                mmMoves++;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                if (mmR1 < 0)
                                {
                                    mmR1 = r; mmC1 = c;
                                }
                                else
                                {
                                    mmR2 = r; mmC2 = c;
                                    if (mmGrid[mmR1, mmC1] == mmGrid[mmR2, mmC2])
                                    {
                                        mmMatched[mmR1, mmC1] = true;
                                        mmMatched[mmR2, mmC2] = true;
                                        mmPairs++;
                                        if (mmPairs >= 8) mmDone = true;
                                        mmR1 = mmC1 = mmR2 = mmC2 = -1;
                                    }
                                    else
                                    {
                                        mmBusy = true;
                                        mmFlipBack = 0.6f;
                                    }
                                }
                            }
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;
            if (mmDone)
            {
                GUI.color = Color.green;
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 4 * cs + 5f, 4 * cs, 25f), $"Complete! {mmMoves} moves in {mmTimer:F1}s", gs);
                GUI.color = Color.white;
            }
        }
        private void LoadMMTextures()
        {
            mmTextures = new Texture2D[mmImagePaths.Length];
            for (int i = 0; i < mmImagePaths.Length; i++)
            {
                try
                {
                    if (File.Exists(mmImagePaths[i]))
                    {
                        byte[] bytes = File.ReadAllBytes(mmImagePaths[i]);
                        mmTextures[i] = new Texture2D(2, 2);
                        mmTextures[i].LoadImage(bytes);
                    }
                }
                catch { mmTextures[i] = null; }
            }
            mmTexturesLoaded = true;
        }
        private void MMNewGame()
        {
            mmGrid = new int[4, 4];
            mmOpen = new bool[4, 4];
            mmMatched = new bool[4, 4];
            mmR1 = mmC1 = mmR2 = mmC2 = -1;
            mmBusy = false; mmPairs = 0; mmMoves = 0; mmDone = false; mmTimer = 0f;
            int numImages = mmImagePaths.Length;
            List<int> vals = new List<int>();
            for (int i = 0; i < numImages; i++) { vals.Add(i); vals.Add(i); }
            int extra = 16 - vals.Count;
            for (int i = 0; i < extra; i++) { int pick = UnityEngine.Random.Range(0, numImages); vals.Add(pick); vals.Add(pick); }
            for (int i = vals.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = vals[i]; vals[i] = vals[j]; vals[j] = tmp;
            }
            int idx = 0;
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 4; c++)
                    mmGrid[r, c] = vals[idx++];
        }

        private void DrawCheckers()
        {
            if (ckBoard == null) CKNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Checkers</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), ckGameOver ? (ckWinner == 1 ? "You Win!" : "AI Wins!") : (ckTurn == 1 ? "Your turn (dark)" : "AI thinking..."));
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { CKNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 38f, ox = 180f, oy = 72f;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    bool dark = (r + c) % 2 == 0;
                    GUI.backgroundColor = dark ? new Color(0.3f, 0.3f, 0.35f) : new Color(0.8f, 0.8f, 0.75f);
                    bool selected = (r == ckSelR && c == ckSelC);
                    if (selected) GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (!ckGameOver && ckTurn == 1)
                        {
                            int piece = ckBoard[r, c];
                            if (piece == 1 || piece == 3)
                            {
                                ckSelR = r; ckSelC = c;
                            }
                            else if (ckSelR >= 0 && piece == 0)
                            {
                                CKTryMove(r, c);
                            }
                        }
                    }
                    int p = ckBoard[r, c];
                    if (p > 0)
                    {
                        GUI.backgroundColor = (p == 1 || p == 3) ? new Color(0.2f, 0.2f, 0.25f) : new Color(0.85f, 0.25f, 0.2f);
                        GUI.Button(new Rect(x + cs * 0.2f, y + cs * 0.2f, cs * 0.6f, cs * 0.6f), "");
                        if (p >= 3)
                        {
                            GUI.color = Color.yellow;
                            GUIStyle ks = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                            GUI.Label(new Rect(x + cs * 0.2f, y + cs * 0.25f, cs * 0.6f, cs * 0.5f), "★", ks);
                            GUI.color = Color.white;
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;
            if (!ckGameOver && ckTurn == 2)
            {
                ckBoard = CKAIMove(ckBoard);
                int jumps = CKMustJump(ckBoard, 2);
                if (jumps > 0) { ckBoard = CKAIMove(ckBoard); }
                ckTurn = 1;
                CKCheckWin();
            }
        }
        private void CKNewGame()
        {
            ckBoard = new int[8, 8];
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if ((r + c) % 2 == 0)
                    {
                        if (r < 3) ckBoard[r, c] = 2;
                        else if (r > 4) ckBoard[r, c] = 1;
                    }
                }
            ckTurn = 1; ckSelR = ckSelC = -1; ckGameOver = false; ckWinner = 0;
        }
        private int CKMustJump(int[,] board, int player)
        {
            int count = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    int p = board[r, c];
                    if (p == 0) continue;
                    bool isPlayer = (player == 1 && (p == 1 || p == 3)) || (player == 2 && (p == 2 || p == 4));
                    if (!isPlayer) continue;
                    int dir = (p == 1 || p == 3) ? -1 : 1;
                    int[] dc = { -1, 1 };
                    for (int d = 0; d < 2; d++)
                    {
                        int mr = r + dir * 2, mc = c + dc[d] * 2;
                        int er = r + dir, ec = c + dc[d];
                        if (mr >= 0 && mr < 8 && mc >= 0 && mc < 8)
                        {
                            bool isOpp = (player == 1 && (board[er, ec] == 2 || board[er, ec] == 4)) ||
                                         (player == 2 && (board[er, ec] == 1 || board[er, ec] == 3));
                            if (isOpp && board[mr, mc] == 0) count++;
                        }
                    }
                }
            return count;
        }
        private void CKTryMove(int tr, int tc)
        {
            int sr = ckSelR, sc = ckSelC;
            int piece = ckBoard[sr, sc];
            int dir = (piece == 1 || piece == 3) ? -1 : 1;
            int dr = tr - sr, dc = tc - sc;
            bool validJump = (Math.Abs(dr) == 2 && Math.Abs(dc) == 2 && (dr == dir * 2 || piece == 3));
            bool validSlide = (Math.Abs(dr) == 1 && Math.Abs(dc) == 1 && dr == dir);
            int mustJump = CKMustJump(ckBoard, 1);
            if (validJump && mustJump > 0)
            {
                int mr = (sr + tr) / 2, mc = (sc + tc) / 2;
                bool isOpp = (ckBoard[mr, mc] == 2 || ckBoard[mr, mc] == 4);
                if (isOpp)
                {
                    ckBoard[tr, tc] = piece;
                    ckBoard[sr, sc] = 0;
                    ckBoard[mr, mc] = 0;
                    if (tr == 0) ckBoard[tr, tc] = 3;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    ckSelR = ckSelC = -1;
                    ckTurn = 2;
                    CKCheckWin();
                }
            }
            else if (validSlide && mustJump == 0)
            {
                ckBoard[tr, tc] = piece;
                ckBoard[sr, sc] = 0;
                if (tr == 0) ckBoard[tr, tc] = 3;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                ckSelR = ckSelC = -1;
                ckTurn = 2;
                CKCheckWin();
            }
        }
        private int[,] CKAIMove(int[,] board)
        {
            List<int[]> jumps = new List<int[]>();
            List<int[]> slides = new List<int[]>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    int p = board[r, c];
                    if (p == 2 || p == 4)
                    {
                        int dir = (p == 2) ? 1 : -1;
                        int[] dcArr = { -1, 1 };
                        for (int d = 0; d < 2; d++)
                        {
                            int tr = r + dir, tc = c + dcArr[d];
                            if (tr >= 0 && tr < 8 && tc >= 0 && tc < 8 && board[tr, tc] == 0)
                                slides.Add(new int[] { r, c, tr, tc });
                            int mr = r + dir, mc = c + dcArr[d];
                            int jr = r + dir * 2, jc = c + dcArr[d] * 2;
                            if (jr >= 0 && jr < 8 && jc >= 0 && jc < 8)
                            {
                                bool isOpp = (board[mr, mc] == 1 || board[mr, mc] == 3);
                                if (isOpp && board[jr, jc] == 0)
                                    jumps.Add(new int[] { r, c, jr, jc });
                            }
                        }
                    }
                }
            List<int[]> list = jumps.Count > 0 ? jumps : slides;
            if (list.Count == 0) return board;
            int[] mv = list[UnityEngine.Random.Range(0, list.Count)];
            int piece = board[mv[0], mv[1]];
            board[mv[2], mv[3]] = piece;
            board[mv[0], mv[1]] = 0;
            if (Math.Abs(mv[2] - mv[0]) == 2)
                board[(mv[0] + mv[2]) / 2, (mv[1] + mv[3]) / 2] = 0;
            if (mv[2] == 7) board[mv[2], mv[3]] = 4;
            return board;
        }
        private void CKCheckWin()
        {
            bool p1 = false, p2 = false;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if (ckBoard[r, c] == 1 || ckBoard[r, c] == 3) p1 = true;
                    if (ckBoard[r, c] == 2 || ckBoard[r, c] == 4) p2 = true;
                }
            if (!p1) { ckGameOver = true; ckWinner = 2; }
            if (!p2) { ckGameOver = true; ckWinner = 1; }
        }

        private void DrawSudoku()
        {
            if (sdkGrid == null) SDKNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Sudoku</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Mistakes: {sdkMistakes}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { SDKNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(520f, 46f, 60f, 22f), "Hint"))
            { SDKHint(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 34f, ox = 185f, oy = 72f;
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    bool sel = (r == sdkSelR && c == sdkSelC);
                    bool fix = sdkFixed[r, c];
                    bool wrong = !fix && sdkGrid[r, c] != 0 && sdkGrid[r, c] != sdkSol[r, c];
                    GUI.backgroundColor = sel ? new Color(0.4f, 0.7f, 1f) :
                        (fix ? new Color(0.25f, 0.25f, 0.3f) :
                        (wrong ? new Color(0.7f, 0.25f, 0.25f) : new Color(0.35f, 0.35f, 0.4f)));
                    string txt = sdkGrid[r, c] > 0 ? sdkGrid[r, c].ToString() : "";
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), txt))
                    {
                        if (!fix) { sdkSelR = r; sdkSelC = c; }
                    }
                    if ((c + 1) % 3 == 0 && c < 8)
                    {
                        GUI.backgroundColor = Color.white;
                        GUI.Button(new Rect(x + cs - 1, y, 2f, cs - 1), "");
                    }
                }
            for (int b = 0; b < 3; b++)
            {
                GUI.backgroundColor = Color.white;
                GUI.Button(new Rect(ox, oy + b * 3 * cs - 1, 9 * cs, 2f), "");
            }
            GUI.backgroundColor = guiColorB;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && sdkSelR >= 0)
            {
                int num = 0;
                if (e.keyCode >= KeyCode.Alpha1 && e.keyCode <= KeyCode.Alpha9) num = e.keyCode - KeyCode.Alpha1 + 1;
                if (e.keyCode >= KeyCode.Keypad1 && e.keyCode <= KeyCode.Keypad9) num = e.keyCode - KeyCode.Keypad1 + 1;
                if (num > 0 && !sdkFixed[sdkSelR, sdkSelC])
                {
                    sdkGrid[sdkSelR, sdkSelC] = num;
                    if (num != sdkSol[sdkSelR, sdkSelC]) sdkMistakes++;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    bool complete = true;
                    for (int r = 0; r < 9; r++)
                        for (int c = 0; c < 9; c++)
                            if (sdkGrid[r, c] != sdkSol[r, c]) complete = false;
                    if (complete)
                    {
                        GUI.color = Color.green;
                        GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                        GUI.Label(new Rect(ox, oy + 9 * cs + 5f, 9 * cs, 25f), "Solved!", gs);
                        GUI.color = Color.white;
                    }
                    e.Use();
                }
                if (e.keyCode == KeyCode.Backspace && sdkSelR >= 0 && !sdkFixed[sdkSelR, sdkSelC])
                {
                    sdkGrid[sdkSelR, sdkSelC] = 0;
                    e.Use();
                }
            }
        }
        private void SDKNewGame()
        {
            sdkSol = new int[9, 9];
            sdkGrid = new int[9, 9];
            sdkFixed = new bool[9, 9];
            int[] baseRow = { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
            for (int i = 8; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); int t = baseRow[i]; baseRow[i] = baseRow[j]; baseRow[j] = t; }
            for (int c = 0; c < 9; c++) sdkSol[0, c] = baseRow[c];
            SDKFill(sdkSol);
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    sdkGrid[r, c] = sdkSol[r, c];
                    sdkFixed[r, c] = true;
                }
            int remove = 40;
            while (remove > 0)
            {
                int r = UnityEngine.Random.Range(0, 9), c = UnityEngine.Random.Range(0, 9);
                if (sdkFixed[r, c]) { sdkFixed[r, c] = false; sdkGrid[r, c] = 0; remove--; }
            }
            sdkMistakes = 0; sdkSelR = sdkSelC = -1;
        }
        private bool SDKFill(int[,] grid)
        {
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                {
                    if (grid[r, c] != 0) continue;
                    bool[] used = new bool[10];
                    for (int i = 0; i < 9; i++)
                    {
                        if (grid[r, i] > 0) used[grid[r, i]] = true;
                        if (grid[i, c] > 0) used[grid[i, c]] = true;
                    }
                    int br = (r / 3) * 3, bc = (c / 3) * 3;
                    for (int dr = 0; dr < 3; dr++)
                        for (int dc = 0; dc < 3; dc++)
                            if (grid[br + dr, bc + dc] > 0) used[grid[br + dr, bc + dc]] = true;
                    List<int> nums = new List<int>();
                    for (int n = 1; n <= 9; n++) if (!used[n]) nums.Add(n);
                    for (int i = nums.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); int t = nums[i]; nums[i] = nums[j]; nums[j] = t; }
                    foreach (int n in nums)
                    {
                        grid[r, c] = n;
                        if (SDKFill(grid)) return true;
                        grid[r, c] = 0;
                    }
                    return false;
                }
            return true;
        }
        private void SDKHint()
        {
            List<int[]> empty = new List<int[]>();
            for (int r = 0; r < 9; r++)
                for (int c = 0; c < 9; c++)
                    if (sdkGrid[r, c] == 0 || sdkGrid[r, c] != sdkSol[r, c])
                        empty.Add(new int[] { r, c });
            if (empty.Count == 0) return;
            int[] pick = empty[UnityEngine.Random.Range(0, empty.Count)];
            sdkGrid[pick[0], pick[1]] = sdkSol[pick[0], pick[1]];
            sdkFixed[pick[0], pick[1]] = true;
        }

        private void DrawTowerDefense()
        {
            if (tdMap == null) TDNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Tower Defense</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Wave: {tdWave}  Lives: {tdLives}  Gold: {tdGold}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { TDNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            tdGoldTimer -= Time.deltaTime;
            if (tdGoldTimer <= 0f) { tdGold += 2; tdGoldTimer = 2f; }
            float cs = 28f, ox = 180f, oy = 72f;
            for (int r = 0; r < 12; r++)
                for (int c = 0; c < 16; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    float cell = tdMap[r, c, 0];
                    GUI.backgroundColor = cell == 0 ? new Color(0.15f, 0.55f, 0.15f) :
                        cell == 1 ? new Color(0.6f, 0.5f, 0.3f) :
                        cell == 2 ? new Color(0.3f, 0.3f, 0.35f) :
                        cell == 3 ? new Color(0.85f, 0.2f, 0.2f) :
                        new Color(0.5f, 0.5f, 0.8f);
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (cell == 0 && tdSelTow >= 0)
                        {
                            float cost = tdTowCost[tdSelTow];
                            if (tdGold >= cost)
                            {
                                tdGold -= (int)cost;
                                tdMap[r, c, 0] = 2;
                                tdMap[r, c, 1] = tdSelTow;
                                tdMap[r, c, 2] = 0;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                        }
                    }
                }
            for (int i = 0; i < tdEnemies.Count; i++)
            {
                float ex = ox + tdEnemies[i].x * cs, ey = oy + tdEnemies[i].y * cs;
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                GUI.Button(new Rect(ex + 2, ey + 2, cs - 5, cs - 5), "");
                float hpPct = tdEnemyHP[i] / tdEnemyMaxHP[i];
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                GUI.Button(new Rect(ex + 2, ey - 3f, (cs - 5) * hpPct, 3f), "");
            }
            for (int r = 0; r < 12; r++)
                for (int c = 0; c < 16; c++)
                {
                    if (tdMap[r, c, 0] == 2)
                    {
                        int ti = (int)tdMap[r, c, 1];
                        float range = tdTowRange[ti];
                        float rr = range / cs;
                        GUI.color = new Color(1f, 1f, 1f, 0.15f);
                        GUI.backgroundColor = Color.clear;
                        GUI.Button(new Rect(ox + (c - rr) * cs, oy + (r - rr) * cs, rr * 2 * cs, rr * 2 * cs), "");
                        GUI.color = Color.white;
                        tdMap[r, c, 2] += Time.deltaTime;
                        if (tdMap[r, c, 2] >= tdTowRate[ti])
                        {
                            int best = -1;
                            float bestDist = float.MaxValue;
                            for (int e = 0; e < tdEnemies.Count; e++)
                            {
                                float dx = tdEnemies[e].x - c, dy = tdEnemies[e].y - r;
                                float d = Mathf.Sqrt(dx * dx + dy * dy);
                                if (d <= rr && d < bestDist) { bestDist = d; best = e; }
                            }
                            if (best >= 0)
                            {
                                tdEnemyHP[best] -= tdTowDmg[ti];
                                tdMap[r, c, 2] = 0;
                            }
                        }
                    }
                }
            for (int i = tdEnemies.Count - 1; i >= 0; i--)
            {
                if (tdEnemyHP[i] <= 0) { tdEnemies.RemoveAt(i); tdEnemyHP.RemoveAt(i); tdEnemyMaxHP.RemoveAt(i); continue; }
                Vector4 e = tdEnemies[i];
                float spd = 1.5f * Time.deltaTime;
                int pr = (int)e.z;
                if (pr == 0) { e.x += spd; if (e.x >= 15) { e.z = 1; e.y++; } }
                else if (pr == 1) { e.y += spd; if (e.y >= 11) { e.z = 2; e.x--; } }
                else { e.x -= spd; if (e.x <= 0) { tdEnemies.RemoveAt(i); tdEnemyHP.RemoveAt(i); tdEnemyMaxHP.RemoveAt(i); tdLives--; if (tdLives <= 0) tdActive = false; } continue; }
                tdEnemies[i] = e;
            }
            if (tdActive && tdSpawned < tdWaveEnemies)
            {
                tdSpawnTimer -= Time.deltaTime;
                if (tdSpawnTimer <= 0f)
                {
                    float hp = 30f + tdWave * 15f;
                    tdEnemies.Add(new Vector4(0f, 1f, 0f, 0f));
                    tdEnemyHP.Add(hp);
                    tdEnemyMaxHP.Add(hp);
                    tdSpawned++;
                    tdSpawnTimer = 1f;
                }
            }
            if (tdActive && tdSpawned >= tdWaveEnemies && tdEnemies.Count == 0)
            {
                tdWave++;
                tdWaveEnemies = 5 + tdWave * 2;
                tdSpawned = 0;
                tdGold += 25;
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + 12 * cs + 5f, 16 * cs, 20f), "<size=11>Click tower type below, then click a grass tile to place. Towers auto-shoot nearest enemy.</size>");
            GUI.backgroundColor = guiColorA;
            for (int i = 0; i < 3; i++)
            {
                bool sel = tdSelTow == i;
                GUI.backgroundColor = sel ? new Color(0.4f, 0.8f, 0.4f) : guiColorA;
                if (GUI.Button(new Rect(ox + i * 140f, oy + 12 * cs + 25f, 130f, 22f), tdTowNames[i]))
                { tdSelTow = i; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            }
            GUI.backgroundColor = guiColorB;
            if (!tdActive && tdLives <= 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 12 * cs + 55f, 16 * cs, 25f), "Game Over! All lives lost.", gs);
                GUI.color = Color.white;
            }
        }
        private void TDNewGame()
        {
            tdMap = new float[12, 16, 3];
            for (int c = 1; c < 15; c++) tdMap[1, c, 0] = 1;
            tdMap[1, 15, 0] = 1; tdMap[1, 15, 2] = 0;
            for (int r = 1; r < 10; r++) tdMap[r, 15, 0] = 1;
            for (int c = 1; c < 16; c++) tdMap[10, c, 0] = 1;
            tdMap[0, 0, 0] = 3;
            tdEnemies = new List<Vector4>();
            tdEnemyHP = new List<float>();
            tdEnemyMaxHP = new List<float>();
            tdWave = 1; tdLives = 10; tdGold = 100;
            tdSelTow = 0; tdActive = true;
            tdSpawned = 0; tdWaveEnemies = 7;
            tdSpawnTimer = 0f; tdGoldTimer = 2f;
        }

        private void DrawMaze()
        {
            if (!mzGenerated) MazeGenerate();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Maze</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), mzDone ? "You escaped! Generate a new maze." : "Reach the green exit!");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Maze"))
            { MazeGenerate(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && !mzDone)
            {
                int[] dr = { -1, 0, 1, 0 }, dc = { 0, 1, 0, -1 };
                int[] walls = { 1, 2, 4, 8 };
                int[] opp = { 4, 8, 1, 2 };
                for (int d = 0; d < 4; d++)
                {
                    if ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) && d != 0) continue;
                    if ((e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) && d != 2) continue;
                    if ((e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) && d != 3) continue;
                    if ((e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) && d != 1) continue;
                    if (e.keyCode != KeyCode.UpArrow && e.keyCode != KeyCode.W && e.keyCode != KeyCode.DownArrow && e.keyCode != KeyCode.S && e.keyCode != KeyCode.LeftArrow && e.keyCode != KeyCode.A && e.keyCode != KeyCode.RightArrow && e.keyCode != KeyCode.D) continue;
                    int nr = mzPR + dr[d], nc = mzPC + dc[d];
                    if (nr >= 0 && nr < mzH && nc >= 0 && nc < mzW && (mzWalls[mzPR, mzPC] & walls[d]) == 0)
                    {
                        mzPR = nr; mzPC = nc;
                        if (mzPR == mzER && mzPC == mzEC) mzDone = true;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    e.Use();
                    break;
                }
            }
            float cs = 22f, ox = 180f, oy = 72f;
            for (int r = 0; r < mzH; r++)
            {
                for (int c = 0; c < mzW; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    GUI.backgroundColor = new Color(0.18f, 0.18f, 0.22f);
                    GUI.Button(new Rect(x, y, cs - 1, cs - 1), "");
                    int w = mzWalls[r, c];
                    GUI.backgroundColor = Color.clear;
                    if ((w & 1) != 0) GUI.Button(new Rect(x, y, cs - 1, 2f), "");
                    if ((w & 2) != 0) GUI.Button(new Rect(x + cs - 3, y, 2f, cs - 1), "");
                    if ((w & 4) != 0) GUI.Button(new Rect(x, y + cs - 3, cs - 1, 2f), "");
                    if ((w & 8) != 0) GUI.Button(new Rect(x, y, 2f, cs - 1), "");
                }
            }
            GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
            GUI.Button(new Rect(ox + mzEC * cs + 3, oy + mzER * cs + 3, cs - 7, cs - 7), "");
            GUI.backgroundColor = new Color(0.3f, 0.5f, 0.9f);
            GUI.Button(new Rect(ox + mzPC * cs + 3, oy + mzPR * cs + 3, cs - 7, cs - 7), "");
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + mzH * cs + 5f, mzW * cs, 20f), "<size=11>Arrow keys or WASD to move</size>");
        }
        private void MazeGenerate()
        {
            mzWalls = new int[mzH, mzW];
            for (int r = 0; r < mzH; r++)
                for (int c = 0; c < mzW; c++)
                    mzWalls[r, c] = 15;
            bool[,] vis = new bool[mzH, mzW];
            Stack<int[]> stack = new Stack<int[]>();
            stack.Push(new int[] { 0, 0 });
            vis[0, 0] = true;
            int[] dr = { -1, 0, 1, 0 }, dc = { 0, 1, 0, -1 };
            int[] walls = { 1, 2, 4, 8 }, opp = { 4, 8, 1, 2 };
            while (stack.Count > 0)
            {
                int[] cur = stack.Peek();
                List<int> neighbors = new List<int>();
                for (int d = 0; d < 4; d++)
                {
                    int nr = cur[0] + dr[d], nc = cur[1] + dc[d];
                    if (nr >= 0 && nr < mzH && nc >= 0 && nc < mzW && !vis[nr, nc])
                        neighbors.Add(d);
                }
                if (neighbors.Count == 0) { stack.Pop(); continue; }
                int dir = neighbors[UnityEngine.Random.Range(0, neighbors.Count)];
                int nr2 = cur[0] + dr[dir], nc2 = cur[1] + dc[dir];
                mzWalls[cur[0], cur[1]] &= ~walls[dir];
                mzWalls[nr2, nc2] &= ~opp[dir];
                vis[nr2, nc2] = true;
                stack.Push(new int[] { nr2, nc2 });
            }
            mzPR = 0; mzPC = 0;
            mzER = mzH - 1; mzEC = mzW - 1;
            mzDone = false;
            mzGenerated = true;
        }

        private void DrawBreakout()
        {
            if (brBricks == null) BRNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Breakout</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {brScore}  Lives: {brLives}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { BRNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float ox = 190f, oy = 72f;
            Event e = Event.current;
            if (e.type == EventType.MouseDown)
                brPaddleX = Mathf.Clamp(e.mousePosition.x - ox - 25f, 0f, brFieldW - 50f);
            if (e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) brPaddleX = Mathf.Max(0f, brPaddleX - 20f);
                if (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) brPaddleX = Mathf.Min(brFieldW - 50f, brPaddleX + 20f);
            }
            if (brActive)
            {
                float dt = Time.deltaTime;
                brBallX += brBallVX * dt;
                brBallY += brBallVY * dt;
                if (brBallX <= 0f || brBallX + 8f >= brFieldW) brBallVX = -brBallVX;
                if (brBallY <= 0f) brBallVY = -brBallVY;
                Rect ballR = new Rect(ox + brBallX, oy + brBallY, 8f, 8f);
                Rect padR = new Rect(ox + brPaddleX, oy + brFieldH - 15f, 50f, 10f);
                if (ballR.Overlaps(padR) && brBallVY > 0f)
                {
                    float hit = (brBallX + 4f - (brPaddleX + 25f)) / 25f;
                    float spd = Mathf.Sqrt(brBallVX * brBallVX + brBallVY * brBallVY);
                    brBallVX = hit * spd;
                    brBallVY = -Mathf.Abs(spd * 0.9f);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                for (int r = 0; r < 5; r++)
                    for (int c = 0; c < 10; c++)
                    {
                        if (!brBricks[r, c]) continue;
                        Rect brickR = new Rect(ox + c * 38f + 2f, oy + 80f + r * 16f, 36f, 14f);
                        if (ballR.Overlaps(brickR))
                        {
                            brBricks[r, c] = false;
                            brBallVY = -brBallVY;
                            brScore += 10;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            break;
                        }
                    }
                if (brBallY > brFieldH) { brLives--; if (brLives <= 0) brActive = false; else BRResetBall(); }
                bool allGone = true;
                for (int r = 0; r < 5; r++)
                    for (int c = 0; c < 10; c++)
                        if (brBricks[r, c]) allGone = false;
                if (allGone) { brActive = false; brScore += 100; }
            }
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
            GUI.Box(new Rect(ox, oy, brFieldW, brFieldH), "");
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 10; c++)
                {
                    if (!brBricks[r, c]) continue;
                    Color bc = new Color[] { Color.red, Color.orange, Color.yellow, Color.green, Color.cyan }[r];
                    GUI.backgroundColor = bc;
                    GUI.Box(new Rect(ox + c * 38f + 2f, oy + 80f + r * 16f, 36f, 14f), "");
                }
            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.85f);
            GUI.Box(new Rect(ox + brPaddleX, oy + brFieldH - 15f, 50f, 10f), "");
            GUI.backgroundColor = Color.yellow;
            GUI.Box(new Rect(ox + brBallX, oy + brBallY, 8f, 8f), "");
            GUI.backgroundColor = guiColorB;
            if (!brActive && brLives > 0)
            {
                GUI.color = Color.green;
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + brFieldH / 2f - 15f, brFieldW, 30f), "You Win!", gs);
                GUI.color = Color.white;
            }
            else if (!brActive && brLives <= 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + brFieldH / 2f - 15f, brFieldW, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + brFieldH + 5f, brFieldW, 16f), "<size=11>Mouse or A/D arrows to move paddle</size>");
        }
        private void BRNewGame()
        {
            brBricks = new bool[5, 10];
            for (int r = 0; r < 5; r++)
                for (int c = 0; c < 10; c++)
                    brBricks[r, c] = true;
            brPaddleX = brFieldW / 2f - 25f;
            brScore = 0; brLives = 3; brActive = true;
            BRResetBall();
        }
        private void BRResetBall()
        {
            brBallX = brFieldW / 2f - 4f;
            brBallY = brFieldH - 40f;
            brBallVX = UnityEngine.Random.Range(-100f, 100f);
            brBallVY = -200f;
        }

        private void DrawMSHard()
        {
            if (mshGrid == null) MSHNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Minesweeper Hard</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Mines: {mshMines}  Flags: {mshFlagsLeft}  Time: {mshTimer:F1}s");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 90f, 22f), mshFlagMode ? "⛏ Dig" : "🚩 Flag"))
            { mshFlagMode = !mshFlagMode; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (GUI.Button(new Rect(530f, 46f, 80f, 22f), "New Game"))
            { MSHNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            if (!mshGameOver && mshStarted) mshTimer += Time.deltaTime;
            float cellSize = 22f;
            float ox = 180f, oy = 72f;
            mshScrollPos = GUI.BeginScrollView(new Rect(170f, 68f, 530f, 340f), mshScrollPos, new Rect(0f, 0f, mshCols * cellSize + 10f, mshRows * cellSize + 10f));
            for (int r = 0; r < mshRows; r++)
            {
                for (int c = 0; c < mshCols; c++)
                {
                    float x = ox + c * cellSize, y = oy + r * cellSize;
                    if (mshRevealed[r, c])
                    {
                        if (mshGrid[r, c] == -1)
                        {
                            GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                            GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), "💣");
                        }
                        else
                        {
                            GUI.backgroundColor = new Color(0.75f, 0.75f, 0.8f);
                            int val = mshGrid[r, c];
                            GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), val > 0 ? val.ToString() : "");
                        }
                    }
                    else if (mshFlagged[r, c])
                    {
                        GUI.backgroundColor = new Color(1f, 0.85f, 0.4f);
                        if (GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), "🚩"))
                        { if (!mshGameOver) { mshFlagged[r, c] = false; mshFlagsLeft++; SoundManager.Play(SoundManager.DefaultSounds["Button"]); } }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.55f, 0.6f, 0.65f);
                        if (GUI.Button(new Rect(x, y, cellSize - 1, cellSize - 1), ""))
                        {
                            if (!mshGameOver)
                            {
                                if (mshFlagMode) { mshFlagged[r, c] = true; mshFlagsLeft--; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                                else { MSHReveal(r, c); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                            }
                        }
                    }
                }
            }
            GUI.EndScrollView();
            GUI.backgroundColor = guiColorB;
            if (mshGameOver)
            {
                GUI.color = mshWon ? Color.green : new Color(1f, 0.4f, 0.4f);
                GUIStyle ms = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(180f, oy + mshRows * 22f + 10f, mshCols * 22f, 25f), mshWon ? "You Win!" : "Game Over!", ms);
                GUI.color = Color.white;
            }
        }
        private void MSHNewGame()
        {
            mshGrid = new int[mshRows, mshCols];
            mshRevealed = new bool[mshRows, mshCols];
            mshFlagged = new bool[mshRows, mshCols];
            int placed = 0;
            while (placed < mshMines)
            {
                int r = UnityEngine.Random.Range(0, mshRows), c = UnityEngine.Random.Range(0, mshCols);
                if (mshGrid[r, c] != -1) { mshGrid[r, c] = -1; placed++; }
            }
            for (int r = 0; r < mshRows; r++)
                for (int c = 0; c < mshCols; c++)
                {
                    if (mshGrid[r, c] == -1) continue;
                    int cnt = 0;
                    for (int dr = -1; dr <= 1; dr++)
                        for (int dc = -1; dc <= 1; dc++)
                        { int nr = r + dr, nc = c + dc; if (nr >= 0 && nr < mshRows && nc >= 0 && nc < mshCols && mshGrid[nr, nc] == -1) cnt++; }
                    mshGrid[r, c] = cnt;
                }
            mshFlagsLeft = mshMines; mshGameOver = false; mshWon = false; mshStarted = false; mshTimer = 0f; mshFlagMode = false;
        }
        private void MSHReveal(int r, int c)
        {
            if (r < 0 || r >= mshRows || c < 0 || c >= mshCols || mshRevealed[r, c] || mshFlagged[r, c]) return;
            mshRevealed[r, c] = true; mshStarted = true;
            if (mshGrid[r, c] == -1)
            {
                mshGameOver = true;
                for (int rr = 0; rr < mshRows; rr++)
                    for (int cc = 0; cc < mshCols; cc++)
                        if (mshGrid[rr, cc] == -1) mshRevealed[rr, cc] = true;
                return;
            }
            if (mshGrid[r, c] == 0)
                for (int dr = -1; dr <= 1; dr++)
                    for (int dc = -1; dc <= 1; dc++)
                        MSHReveal(r + dr, c + dc);
            int unrevealed = 0;
            for (int rr = 0; rr < mshRows; rr++)
                for (int cc = 0; cc < mshCols; cc++)
                    if (!mshRevealed[rr, cc] && mshGrid[rr, cc] != -1) unrevealed++;
            if (unrevealed == 0) { mshGameOver = true; mshWon = true; }
        }

        private void DrawChineseCheckers()
        {
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Chinese Checkers</size>");
            GUI.Label(new Rect(170f, 48f, 500f, 20f), ccGameOver ? "<size=12>Congratulations! Click New Game to play again</size>" : $"<size=12>Moves: {ccMoves}  |  Click a green piece, then click destination</size>");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { CCNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            if (ccBoard == null) CCNewGame();

            float cs = 32f, ox = 210f, oy = 72f;
            for (int r = 0; r < ccBoardSize; r++)
            {
                for (int c = 0; c < ccBoardSize; c++)
                {
                    bool valid = CCIsValid(r, c);
                    if (!valid) continue;
                    float x = ox + c * cs, y = oy + r * cs;
                    int val = ccBoard[r, c];
                    Color bgColor;
                    if (val == 1)
                        bgColor = new Color(0.2f, 0.7f, 0.3f);
                    else if (val == 2)
                        bgColor = new Color(0.85f, 0.25f, 0.25f);
                    else if (val == 3)
                        bgColor = new Color(0.2f, 0.2f, 0.85f);
                    else
                        bgColor = new Color(0.3f, 0.3f, 0.35f);

                    bool selected = (r == ccSelR && c == ccSelC);
                    if (selected)
                        bgColor = new Color(1f, 1f, 0.3f);

                    GUI.backgroundColor = bgColor;
                    string label = val == 1 ? "G" : val == 2 ? "R" : val == 3 ? "B" : "";
                    if (GUI.Button(new Rect(x, y, cs - 2, cs - 2), label))
                    {
                        if (ccGameOver) continue;
                        if (val == 1)
                        {
                            ccSelR = r;
                            ccSelC = c;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                        else if (ccSelR >= 0)
                        {
                            bool moved = false;
                            if (val == 0 && CCIsAdjacent(ccSelR, ccSelC, r, c))
                            {
                                ccBoard[r, c] = 1;
                                ccBoard[ccSelR, ccSelC] = 0;
                                moved = true;
                            }
                            else if (val != 0 && CCIsAdjacent(ccSelR, ccSelC, r, c))
                            {
                                int jr = r + (r - ccSelR), jc = c + (c - ccSelC);
                                if (CCIsValid(jr, jc) && ccBoard[jr, jc] == 0)
                                {
                                    ccBoard[jr, jc] = 1;
                                    ccBoard[ccSelR, ccSelC] = 0;
                                    moved = true;
                                    r = jr; c = jc;
                                }
                            }
                            if (moved)
                            {
                                ccSelR = ccSelC = -1;
                                ccMoves++;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                CCPlayerMoved();
                            }
                        }
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + ccBoardSize * cs + 8f, ccBoardSize * cs, 20f), "<size=11>Green=You  Red=AI  Blue=Obstacles  |  Click green, then click empty neighbor</size>");
        }
        private bool CCIsValid(int r, int c)
        {
            int half = ccBoardSize / 2;
            if (r < 0 || r >= ccBoardSize || c < 0 || c >= ccBoardSize) return false;
            int dr = Mathf.Abs(r - half);
            int dc = Mathf.Abs(c - half);
            return dr + dc <= half + 1;
        }
        private bool CCIsAdjacent(int r1, int c1, int r2, int c2)
        {
            int dr = Mathf.Abs(r1 - r2), dc = Mathf.Abs(c1 - c2);
            return (dr <= 1 && dc <= 1) && (dr + dc > 0);
        }
        private void CCNewGame()
        {
            ccBoard = new int[ccBoardSize, ccBoardSize];
            int half = ccBoardSize / 2;
            for (int r = 0; r < ccBoardSize; r++)
                for (int c = 0; c < ccBoardSize; c++)
                    ccBoard[r, c] = 0;
            for (int i = 0; i < 3; i++)
            {
                ccBoard[ccBoardSize - 1, half - 1 + i] = 1;
                ccBoard[0, half - 1 + i] = 2;
                if (i < 2)
                {
                    ccBoard[ccBoardSize - 2, half + i] = 3;
                    ccBoard[1, half - 1 + i] = 3;
                }
            }
            ccPlayerPieces = 3;
            ccAIPieces = 3;
            ccSelR = ccSelC = -1;
            ccMoves = 0;
            ccGameOver = false;
        }
        private bool CCCanJump(int r1, int c1, int r2, int c2, int lr, int lc)
        {
            int mr = (r1 + r2) / 2, mc = (c1 + c2) / 2;
            return ccBoard[mr, mc] != 0 && ccBoard[lr, lc] == 0 && CCIsValid(lr, lc);
        }
        private void CCPlayerMoved()
        {
            int half = ccBoardSize / 2;
            int playerAtTop = 0;
            for (int c = half - 1; c <= half + 1; c++)
                if (ccBoard[0, c] == 1) playerAtTop++;
            if (playerAtTop >= 3) { ccGameOver = true; return; }
            CCAIMove();
        }
        private void CCAIMove()
        {
            int half = ccBoardSize / 2;
            List<int[]> aiPieces = new List<int[]>();
            for (int r = 0; r < ccBoardSize; r++)
                for (int c = 0; c < ccBoardSize; c++)
                    if (ccBoard[r, c] == 2) aiPieces.Add(new int[] { r, c });
            if (aiPieces.Count == 0) return;
            int[] bestPiece = aiPieces[0];
            int bestDist = 999;
            int bestR = -1, bestC = -1;
            foreach (int[] p in aiPieces)
            {
                int[] dirs = { -1, 0, 1 };
                foreach (int dr in dirs)
                    foreach (int dc in dirs)
                    {
                        if (dr == 0 && dc == 0) continue;
                        int nr = p[0] + dr, nc = p[1] + dc;
                        if (CCIsValid(nr, nc) && ccBoard[nr, nc] == 0)
                        {
                            int dist = Mathf.Abs(nr - (ccBoardSize - 1)) + Mathf.Abs(nc - half);
                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                bestPiece = p;
                                bestR = nr;
                                bestC = nc;
                            }
                        }
                    }
                for (int dr2 = -2; dr2 <= 2; dr2++)
                    {
                        for (int dc2 = -2; dc2 <= 2; dc2++)
                        {
                            if (dr2 == 0 && dc2 == 0) continue;
                            if (Mathf.Abs(dr2) > 1 || Mathf.Abs(dc2) > 1)
                            {
                                int lr = p[0] + dr2, lc = p[1] + dc2;
                                int mr = p[0] + dr2 / 2, mc = p[1] + dc2 / 2;
                                if (CCIsValid(lr, lc) && ccBoard[lr, lc] == 0 && CCIsValid(mr, mc) && ccBoard[mr, mc] != 0)
                                {
                                    int dist = Mathf.Abs(lr - (ccBoardSize - 1)) + Mathf.Abs(lc - half);
                                    if (dist < bestDist)
                                    {
                                        bestDist = dist;
                                        bestPiece = p;
                                        bestR = lr;
                                        bestC = lc;
                                    }
                                }
                            }
                        }
                    }
            }
            if (bestR >= 0)
            {
                ccBoard[bestR, bestC] = 2;
                ccBoard[bestPiece[0], bestPiece[1]] = 0;
                int atBot = 0;
                for (int c = half - 1; c <= half + 1; c++)
                    if (ccBoard[ccBoardSize - 1, c] == 2) atBot++;
                if (atBot >= 3) ccGameOver = true;
            }
        }

        private void DrawTetris()
        {
            if (tetGrid == null) TetNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Tetris</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {tetScore}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { TetNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 22f, ox = 240f, oy = 72f;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && tetActive)
            {
                if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A)
                { if (TetCanMove(tetX - 1, tetY, tetPiece, tetRot)) tetX--; e.Use(); }
                else if (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D)
                { if (TetCanMove(tetX + 1, tetY, tetPiece, tetRot)) tetX++; e.Use(); }
                else if (e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W)
                { int nr = (tetRot + 1) % 4; if (TetCanMove(tetX, tetY, tetPiece, nr)) tetRot = nr; e.Use(); }
                else if (e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S)
                { while (TetCanMove(tetX, tetY + 1, tetPiece, tetRot)) tetY++; TetPlace(); e.Use(); }
            }
            if (tetActive)
            {
                tetTimer += Time.deltaTime;
                float interval = Mathf.Max(0.1f, 0.5f - tetScore * 0.002f);
                if (tetTimer >= interval)
                {
                    tetTimer = 0f;
                    if (TetCanMove(tetX, tetY + 1, tetPiece, tetRot)) tetY++;
                    else TetPlace();
                }
            }
            for (int r = 0; r < 20; r++)
                for (int c = 0; c < 10; c++)
                {
                    bool filled = tetGrid[r, c] >= 0;
                    if (filled)
                    {
                        GUI.backgroundColor = tetColors[tetGrid[r, c]];
                        GUI.Box(new Rect(ox + c * cs, oy + r * cs, cs - 1, cs - 1), "");
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
                        GUI.Box(new Rect(ox + c * cs, oy + r * cs, cs - 1, cs - 1), "");
                    }
                }
            int[,] shape = tetShapes[tetPiece];
            int sh = shape.GetLength(0), sw = shape.GetLength(1);
            GUI.backgroundColor = tetColors[tetPiece];
            for (int r = 0; r < sh; r++)
                for (int c = 0; c < sw; c++)
                    if (shape[r, c] == 1)
                        GUI.Box(new Rect(ox + (tetX + c) * cs, oy + (tetY + r) * cs, cs - 1, cs - 1), "");
            if (tetNext >= 0)
            {
                GUI.Label(new Rect(ox + 10 * cs + 10f, oy, 80f, 20f), "Next:");
                int[,] ns = tetShapes[tetNext];
                GUI.backgroundColor = tetColors[tetNext];
                for (int r = 0; r < ns.GetLength(0); r++)
                    for (int c = 0; c < ns.GetLength(1); c++)
                        if (ns[r, c] == 1)
                            GUI.Box(new Rect(ox + 10 * cs + 10f + c * cs, oy + 22f + r * cs, cs - 1, cs - 1), "");
            }
            GUI.backgroundColor = guiColorB;
            if (!tetActive)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 10 * cs, 10 * cs, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + 20 * cs + 5f, 10 * cs, 20f), "<size=11>Arrows/WASD: Move, Up/W: Rotate, Down/S: Drop</size>");
        }
        private void TetNewGame()
        {
            tetGrid = new int[20, 10];
            for (int r = 0; r < 20; r++)
                for (int c = 0; c < 10; c++)
                    tetGrid[r, c] = -1;
            tetScore = 0; tetActive = true; tetTimer = 0f; tetSpeed = 0.5f;
            tetPiece = UnityEngine.Random.Range(0, 7);
            tetNext = UnityEngine.Random.Range(0, 7);
            tetX = 3; tetY = 0; tetRot = 0;
        }
        private bool TetCanMove(int tx, int ty, int piece, int rot)
        {
            int[,] shape = tetShapes[piece];
            int sh = shape.GetLength(0), sw = shape.GetLength(1);
            for (int r = 0; r < sh; r++)
                for (int c = 0; c < sw; c++)
                {
                    if (shape[r, c] == 0) continue;
                    int nx = tx + c, ny = ty + r;
                    if (nx < 0 || nx >= 10 || ny >= 20) return false;
                    if (ny >= 0 && tetGrid[ny, nx] >= 0) return false;
                }
            return true;
        }
        private void TetPlace()
        {
            int[,] shape = tetShapes[tetPiece];
            int sh = shape.GetLength(0), sw = shape.GetLength(1);
            for (int r = 0; r < sh; r++)
                for (int c = 0; c < sw; c++)
                    if (shape[r, c] == 1 && tetY + r >= 0)
                        tetGrid[tetY + r, tetX + c] = tetPiece;
            int lines = 0;
            for (int r = 19; r >= 0; r--)
            {
                bool full = true;
                for (int c = 0; c < 10; c++)
                    if (tetGrid[r, c] < 0) { full = false; break; }
                if (full)
                {
                    lines++;
                    for (int rr = r; rr > 0; rr--)
                        for (int c = 0; c < 10; c++)
                            tetGrid[rr, c] = tetGrid[rr - 1, c];
                    for (int c = 0; c < 10; c++) tetGrid[0, c] = -1;
                    r++;
                }
            }
            if (lines > 0) tetScore += lines * lines * 100;
            tetPiece = tetNext;
            tetNext = UnityEngine.Random.Range(0, 7);
            tetX = 3; tetY = 0; tetRot = 0;
            if (!TetCanMove(tetX, tetY, tetPiece, tetRot)) tetActive = false;
        }

        private void DrawSolitaire()
        {
            if (solColumns == null) SolNewGame();
            bool solWin = true;
            foreach (var f in solFoundation)
                if (f.Count < 13) solWin = false;
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Solitaire</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), solWin && solFoundation[0].Count > 0 ? "<size=12>You Win!</size>" : $"<size=12>Stock: {solStock.Count}  |  Foundation cards: {SolFoundationTotal()}</size>");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { SolNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cw = 52f, ch = 72f, ox = 175f, oy = 72f, gap = 56f;
            string[] suits = { "\u2665", "\u2666", "\u2663", "\u2660" };
            Color[] suitColors = { Color.red, Color.red, Color.black, Color.black };
            string[] ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            for (int i = 0; i < 4; i++)
            {
                float fx = ox + (i + 3) * gap;
                GUI.backgroundColor = new Color(0.15f, 0.45f, 0.15f);
                GUI.Box(new Rect(fx, oy, cw, ch), "");
                if (solFoundation[i].Count > 0)
                {
                    int top = solFoundation[i][solFoundation[i].Count - 1];
                    int s = top / 13, r = top % 13;
                    GUI.backgroundColor = Color.white;
                    GUI.color = suitColors[s];
                    GUIStyle cs2 = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
                    GUI.Label(new Rect(fx + 2, oy + 2, cw - 4, 18f), $"{ranks[r]}{suits[s]}", cs2);
                    GUIStyle ss = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                    GUI.Label(new Rect(fx, oy + 18f, cw, 30f), suits[s], ss);
                    GUI.color = Color.white;
                }
            }
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
            if (solStock.Count > 0)
            {
                if (GUI.Button(new Rect(ox, oy, cw, ch), "🂠"))
                {
                    int count = Mathf.Min(3, solStock.Count);
                    for (int i = 0; i < count; i++)
                    {
                        solWastePile.Add(solStock[solStock.Count - 1]);
                        solStock.RemoveAt(solStock.Count - 1);
                    }
                    solWasteActive = solWastePile.Count > 0;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            else
            {
                if (GUI.Button(new Rect(ox, oy, cw, ch), "↻"))
                {
                    solWastePile.Reverse();
                    solStock.AddRange(solWastePile);
                    solWastePile.Clear();
                    solWasteActive = false;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            if (solWastePile.Count > 0)
            {
                int top = solWastePile[solWastePile.Count - 1];
                int s = top / 13, r = top % 13;
                GUI.backgroundColor = (solSelectedCol == -2) ? new Color(0.6f, 0.8f, 1f) : Color.white;
                if (GUI.Button(new Rect(ox + gap, oy, cw, ch), ""))
                {
                    if (solSelectedCol == -2)
                    {
                        solSelectedCol = -1;
                        solSelectedIdx = -1;
                    }
                    else
                    {
                        solSelectedCol = -2;
                        solSelectedIdx = solWastePile.Count - 1;
                    }
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                GUI.color = suitColors[s];
                GUIStyle ws = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
                GUI.Label(new Rect(ox + gap + 2, oy + 2, cw - 4, 18f), $"{ranks[r]}{suits[s]}", ws);
                GUIStyle wss = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox + gap, oy + 18f, cw, 30f), suits[s], wss);
                GUI.color = Color.white;
            }
            for (int col = 0; col < 7; col++)
            {
                float cx = ox + col * gap;
                float cy = oy + ch + 10f;
                int count = solColumns[col].Count;
                for (int idx = 0; idx < count; idx++)
                {
                    int card = solColumns[col][idx];
                    bool faceUp = solColFaceUp[col][idx];
                    float cardY = cy + idx * 18f;
                    bool isSelected = (solSelectedCol == col && solSelectedIdx == idx);
                    if (isSelected)
                        GUI.backgroundColor = new Color(0.6f, 0.8f, 1f);
                    else if (faceUp)
                        GUI.backgroundColor = Color.white;
                    else
                        GUI.backgroundColor = new Color(0.2f, 0.3f, 0.6f);
                    if (GUI.Button(new Rect(cx, cardY, cw, ch - 10f), ""))
                    {
                        if (faceUp)
                        {
                            if (solSelectedCol >= 0 && solSelectedCol != col)
                            {
                                if (SolCanPlaceOnColumn(col, solSelectedCol, solSelectedIdx))
                                {
                                    SolMoveCards(col, solSelectedCol, solSelectedIdx);
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                else
                                {
                                    solSelectedCol = col;
                                    solSelectedIdx = idx;
                                }
                            }
                            else if (solSelectedCol == -2)
                            {
                                int wasteCard = solWastePile[solWastePile.Count - 1];
                                if (SolCanPlaceOnColumnFromCard(col, wasteCard))
                                {
                                    solColumns[col].Add(wasteCard);
                                    solWastePile.RemoveAt(solWastePile.Count - 1);
                                    solWasteActive = solWastePile.Count > 0;
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                solSelectedCol = -1;
                                solSelectedIdx = -1;
                            }
                            else
                            {
                                solSelectedCol = col;
                                solSelectedIdx = idx;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                        }
                    }
                    if (faceUp)
                    {
                        int s = card / 13, r = card % 13;
                        GUI.color = suitColors[s];
                        GUIStyle fs = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold };
                        GUI.Label(new Rect(cx + 2, cardY + 2, cw - 4, 14f), $"{ranks[r]}{suits[s]}", fs);
                        GUIStyle ff = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                        GUI.Label(new Rect(cx, cardY + 14f, cw, 24f), suits[s], ff);
                        GUI.color = Color.white;
                    }
                }
                if (count == 0)
                {
                    GUI.backgroundColor = new Color(0.35f, 0.35f, 0.4f);
                    if (GUI.Button(new Rect(cx, cy, cw, ch - 10f), ""))
                    {
                        if (solSelectedCol == -2)
                        {
                            int wasteCard = solWastePile[solWastePile.Count - 1];
                            int ws2 = wasteCard / 13, wr = wasteCard % 13;
                            if (wr == 0)
                            {
                                solColumns[col].Add(wasteCard);
                                solWastePile.RemoveAt(solWastePile.Count - 1);
                                solWasteActive = solWastePile.Count > 0;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                            solSelectedCol = -1;
                            solSelectedIdx = -1;
                        }
                        else if (solSelectedCol >= 0)
                        {
                            int card = solColumns[solSelectedCol][solSelectedIdx];
                            int s = card / 13, r = card % 13;
                            if (r == 12)
                            {
                                SolMoveCards(col, solSelectedCol, solSelectedIdx);
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                        }
                    }
                }
            }
            for (int f = 0; f < 4; f++)
            {
                float fx = ox + (f + 3) * gap, fy = oy;
                if (GUI.Button(new Rect(fx, fy, cw, ch), ""))
                {
                    if (solSelectedCol == -2)
                    {
                        int wasteCard = solWastePile[solWastePile.Count - 1];
                        if (SolCanPlaceOnFoundation(f, wasteCard))
                        {
                            solFoundation[f].Add(wasteCard);
                            solWastePile.RemoveAt(solWastePile.Count - 1);
                            solWasteActive = solWastePile.Count > 0;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                        solSelectedCol = -1;
                        solSelectedIdx = -1;
                    }
                    else if (solSelectedCol >= 0 && solColumns[solSelectedCol].Count > 0)
                    {
                        int card = solColumns[solSelectedCol][solSelectedCol == solSelectedCol ? solSelectedIdx : solColumns[solSelectedCol].Count - 1];
                        if (solSelectedIdx == solColumns[solSelectedCol].Count - 1 && SolCanPlaceOnFoundation(f, card))
                        {
                            solFoundation[f].Add(card);
                            solColumns[solSelectedCol].RemoveAt(solSelectedCol == solSelectedCol ? solSelectedIdx : solColumns[solSelectedCol].Count - 1);
                            if (solColumns[solSelectedCol].Count > 0)
                                solColFaceUp[solSelectedCol][solColumns[solSelectedCol].Count - 1] = true;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                        solSelectedCol = -1;
                        solSelectedIdx = -1;
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + ch + 10f + 7 * 18f + ch + 5f, 500f, 20f), "<size=11>Click stock to draw. Click card to select, then click column/foundation to place. Click foundation to auto-move.</size>");
        }
        private bool SolCanPlaceOnFoundation(int f, int card)
        {
            int s = card / 13, r = card % 13;
            if (solFoundation[f].Count == 0) return r == 0;
            int top = solFoundation[f][solFoundation[f].Count - 1];
            return top / 13 == s && (top % 13) == r - 1;
        }
        private bool SolCanPlaceOnColumn(int targetCol, int fromCol, int fromIdx)
        {
            int card = solColumns[fromCol][fromIdx];
            if (solColumns[targetCol].Count == 0)
            {
                int s = card / 13, r = card % 13;
                return r == 12;
            }
            int topCard = solColumns[targetCol][solColumns[targetCol].Count - 1];
            int ts = topCard / 13, tr = topCard % 13;
            int cs2 = card / 13, cr = card % 13;
            return (ts + cs2) % 2 == 1 && tr == cr + 1;
        }
        private bool SolCanPlaceOnColumnFromCard(int targetCol, int card)
        {
            if (solColumns[targetCol].Count == 0)
            {
                int s = card / 13, r = card % 13;
                return r == 12;
            }
            int topCard = solColumns[targetCol][solColumns[targetCol].Count - 1];
            int ts = topCard / 13, tr = topCard % 13;
            int cs2 = card / 13, cr = card % 13;
            return (ts + cs2) % 2 == 1 && tr == cr + 1;
        }
        private void SolMoveCards(int targetCol, int fromCol, int fromIdx)
        {
            int count = solColumns[fromCol].Count - fromIdx;
            solColumns[targetCol].AddRange(solColumns[fromCol].GetRange(fromIdx, count));
            solColumns[fromCol].RemoveRange(fromIdx, count);
            if (solColumns[fromCol].Count > 0)
                solColFaceUp[fromCol][solColumns[fromCol].Count - 1] = true;
            solSelectedCol = -1;
            solSelectedIdx = -1;
        }
        private int SolFoundationTotal()
        {
            int total = 0;
            foreach (var f in solFoundation) total += f.Count;
            return total;
        }
        private void SolNewGame()
        {
            solColumns = new List<int>[7];
            solColFaceUp = new List<bool>[7];
            for (int i = 0; i < 7; i++)
            {
                solColumns[i] = new List<int>();
                solColFaceUp[i] = new List<bool>();
            }
            solFoundation = new List<int>[4];
            for (int i = 0; i < 4; i++) solFoundation[i] = new List<int>();
            solStock = new List<int>();
            solWastePile = new List<int>();
            solWasteActive = false;
            solSelectedCol = -1;
            solSelectedIdx = -1;
            List<int> deck = new List<int>();
            for (int i = 0; i < 52; i++) deck.Add(i);
            for (int i = deck.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                int tmp = deck[i]; deck[i] = deck[j]; deck[j] = tmp;
            }
            int idx = 0;
            for (int col = 0; col < 7; col++)
            {
                for (int row = 0; row <= col; row++)
                {
                    solColumns[col].Add(deck[idx]);
                    solColFaceUp[col].Add(row == col);
                    idx++;
                }
            }
            while (idx < 52)
            {
                solStock.Add(deck[idx]);
                idx++;
            }
        }

        private void DrawChess()
        {
            if (chBoard == null) ChNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Chess</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), chGameOver ? (chWinner == 1 ? "You Win!" : "AI Wins!") : (chTurn == 1 ? "Your turn (White)" : "AI thinking..."));
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { ChNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 40f, ox = 180f, oy = 72f;
            string[] pieceChars = { "", "♙", "♖", "♘", "♗", "♕", "♔", "♟", "♜", "♞", "♝", "♛", "♚" };
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    bool dark = (r + c) % 2 == 0;
                    bool sel = (r == chSelR && c == chSelC);
                    GUI.backgroundColor = sel ? new Color(0.4f, 0.8f, 0.4f) : (dark ? new Color(0.45f, 0.35f, 0.25f) : new Color(0.9f, 0.85f, 0.7f));
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (chGameOver || chTurn != 1) continue;
                        int p = chBoard[r, c];
                        if (chSelR < 0)
                        {
                            if (p > 0 && p <= 6) { chSelR = r; chSelC = c; }
                        }
                        else
                        {
                            if (p > 0 && p <= 6) { chSelR = r; chSelC = c; }
                            else { ChTryMove(r, c); }
                        }
                    }
                    int piece = chBoard[r, c];
                    if (piece > 0)
                    {
                        GUI.color = piece <= 6 ? Color.white : Color.black;
                        GUIStyle ps = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                        GUI.Label(new Rect(x, y + 2f, cs, cs), pieceChars[piece], ps);
                        GUI.color = Color.white;
                    }
                }
            GUI.backgroundColor = guiColorB;
            if (!chGameOver && chTurn == 2)
            {
                ChAIMove();
                chTurn = 1;
                ChCheckWin();
            }
        }
        private void ChNewGame()
        {
            chBoard = new int[8, 8];
            int[] back = { 2, 3, 4, 5, 6, 4, 3, 2 };
            for (int c = 0; c < 8; c++)
            {
                chBoard[0, c] = back[c];
                chBoard[1, c] = 1;
                chBoard[6, c] = 7;
                chBoard[7, c] = back[c] + 6;
            }
            chTurn = 1; chSelR = chSelC = -1; chGameOver = false; chWinner = 0;
        }
        private void ChTryMove(int tr, int tc)
        {
            int sr = chSelR, sc = chSelC;
            int piece = chBoard[sr, sc];
            int target = chBoard[tr, tc];
            if (target > 0 && target <= 6) { chSelR = sr; chSelC = sc; return; }
            int dr = tr - sr, dc = tc - sc;
            bool valid = false;
            int pt = piece;
            if (pt == 1)
            {
                if (dc == 0 && dr == -1 && target == 0) valid = true;
                if (dc == 0 && dr == -2 && sr == 6 && chBoard[sr - 1, sc] == 0 && target == 0) valid = true;
                if (Math.Abs(dc) == 1 && dr == -1 && target > 6) valid = true;
            }
            else if (pt == 2 || pt == 8)
            {
                if (dr == 0 || dc == 0) { valid = ChPathClear(sr, sc, tr, tc); }
            }
            else if (pt == 3 || pt == 9)
            {
                bool isL = (Math.Abs(dr) == 2 && Math.Abs(dc) == 1) || (Math.Abs(dr) == 1 && Math.Abs(dc) == 2);
                if (isL && (target == 0 || (pt == 3 ? target > 6 : target > 0 && target <= 6))) valid = true;
            }
            else if (pt == 4 || pt == 10)
            {
                if (Math.Abs(dr) == Math.Abs(dc)) valid = ChPathClear(sr, sc, tr, tc);
            }
            else if (pt == 5 || pt == 11)
            {
                if (dr == 0 || dc == 0 || Math.Abs(dr) == Math.Abs(dc)) valid = ChPathClear(sr, sc, tr, tc);
            }
            else if (pt == 6 || pt == 12)
            {
                if (Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1) valid = true;
            }
            if (valid)
            {
                chBoard[tr, tc] = piece;
                chBoard[sr, sc] = 0;
                if (pt == 1 && tr == 0) chBoard[tr, tc] = 5;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                chSelR = chSelC = -1;
                chTurn = 2;
            }
        }
        private bool ChPathClear(int sr, int sc, int tr, int tc)
        {
            int dr = Math.Sign(tr - sr), dc = Math.Sign(tc - sc);
            int r = sr + dr, c = sc + dc;
            while (r != tr || c != tc)
            {
                if (chBoard[r, c] != 0) return false;
                r += dr; c += dc;
            }
            return true;
        }
        private void ChAIMove()
        {
            List<int[]> moves = new List<int[]>();
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    int p = chBoard[r, c];
                    if (p < 7) continue;
                    for (int tr = 0; tr < 8; tr++)
                        for (int tc = 0; tc < 8; tc++)
                        {
                            if (tr == r && tc == c) continue;
                            int tgt = chBoard[tr, tc];
                            if (tgt > 0 && tgt <= 6) continue;
                            int dr = tr - r, dc = tc - c;
                            bool valid = false;
                            if (p == 7) { if (dc == 0 && dr == 1 && tgt == 0) valid = true; if (Math.Abs(dc) == 1 && dr == 1 && tgt > 0 && tgt <= 6) valid = true; }
                            else if (p == 8 || p == 14) { if (dr == 0 || dc == 0) valid = ChPathClear(r, c, tr, tc); }
                            else if (p == 9 || p == 15) { bool isL2 = (Math.Abs(dr) == 2 && Math.Abs(dc) == 1) || (Math.Abs(dr) == 1 && Math.Abs(dc) == 2); if (isL2 && (tgt == 0 || tgt <= 6)) valid = true; }
                            else if (p == 10 || p == 16) { if (Math.Abs(dr) == Math.Abs(dc)) valid = ChPathClear(r, c, tr, tc); }
                            else if (p == 11 || p == 17) { if (dr == 0 || dc == 0 || Math.Abs(dr) == Math.Abs(dc)) valid = ChPathClear(r, c, tr, tc); }
                            else if (p == 12 || p == 18) { if (Math.Abs(dr) <= 1 && Math.Abs(dc) <= 1) valid = true; }
                            if (valid) moves.Add(new int[] { r, c, tr, tc });
                        }
                }
            if (moves.Count == 0) return;
            int[] best = moves[0];
            int bestScore = -1;
            foreach (int[] mv in moves)
            {
                int cap = chBoard[mv[2], mv[3]];
                int sc = cap > 0 && cap <= 6 ? 10 : 0;
                if (sc > bestScore) { bestScore = sc; best = mv; }
            }
            chBoard[best[2], best[3]] = chBoard[best[0], best[1]];
            chBoard[best[0], best[1]] = 0;
            int mp = chBoard[best[2], best[3]];
            if (mp == 7 && best[2] == 7) chBoard[best[2], best[3]] = 11;
            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
        }
        private void ChCheckWin()
        {
            bool w = false, b = false;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if (chBoard[r, c] == 6) w = true;
                    if (chBoard[r, c] == 12) b = true;
                }
            if (!w) { chGameOver = true; chWinner = 2; }
            if (!b) { chGameOver = true; chWinner = 1; }
        }

        private void DrawWhackAMole()
        {
            if (!wamActive && wamScore == 0) WAMNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Whack-a-Mole</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {wamScore}  Lives: {wamLives}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { WAMNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            wamTimer += Time.deltaTime;
            wamSpawnTimer += Time.deltaTime;
            if (wamActive)
            {
                if (wamSpawnTimer >= 0.8f)
                {
                    wamSpawnTimer = 0f;
                    int r = UnityEngine.Random.Range(0, 3), c = UnityEngine.Random.Range(0, 3);
                    wamGrid[r, c] = 1;
                    float speed = Mathf.Max(0.4f, 1.2f - wamScore * 0.03f);
                    wamMoleTimer = speed;
                }
                wamMoleTimer -= Time.deltaTime;
                if (wamMoleTimer <= 0f)
                {
                    for (int r = 0; r < 3; r++)
                        for (int c = 0; c < 3; c++)
                            if (wamGrid[r, c] == 1) wamGrid[r, c] = 0;
                    wamLives--;
                    if (wamLives <= 0) wamActive = false;
                }
            }
            float cs = 70f, ox = 200f, oy = 80f;
            for (int r = 0; r < 3; r++)
            {
                for (int c = 0; c < 3; c++)
                {
                    float x = ox + c * (cs + 8f), y = oy + r * (cs + 8f);
                    if (wamGrid[r, c] == 1)
                    {
                        GUI.backgroundColor = new Color(0.6f, 0.4f, 0.2f);
                        if (GUI.Button(new Rect(x, y, cs, cs), "<size=28>🐹</size>"))
                        {
                            wamGrid[r, c] = 0;
                            wamScore += 10;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.35f, 0.55f, 0.25f);
                        GUI.Box(new Rect(x, y, cs, cs), "");
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            if (!wamActive && wamScore > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 3 * (cs + 8f) + 10f, 3 * cs + 16f, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + 3 * (cs + 8f) + 45f, 3 * cs + 16f, 20f), "<size=11>Click moles as they appear!</size>");
        }
        private void WAMNewGame()
        {
            wamGrid = new int[3, 3];
            wamScore = 0; wamLives = 5; wamActive = true;
            wamTimer = 0f; wamSpawnTimer = 0f; wamMoleTimer = 1f;
        }

        private void DrawReactionTest()
        {
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Reaction Test</size>");
            if (rtBest > 0f)
                GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Best: {rtBest * 1000f:F0}ms");
            float ox = 200f, oy = 80f, w = 400f, h = 250f;
            GUI.backgroundColor = guiColorB;
            Event e = Event.current;
            if (rtState == 0)
            {
                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
                if (GUI.Button(new Rect(ox, oy, w, h), "<size=22>Click to Start</size>"))
                {
                    rtState = 1;
                    rtWaitTime = UnityEngine.Random.Range(1f, 4f);
                    rtTimer = 0f;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            else if (rtState == 1)
            {
                GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
                GUI.Box(new Rect(ox, oy, w, h), "<size=22>Wait for green...</size>");
                rtTimer += Time.deltaTime;
                if (rtTimer >= rtWaitTime)
                {
                    rtState = 2;
                    rtTimer = 0f;
                }
            }
            else if (rtState == 2)
            {
                GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
                if (GUI.Button(new Rect(ox, oy, w, h), "<size=22>CLICK NOW!</size>"))
                {
                    float time = rtTimer;
                    if (rtBest <= 0f || time < rtBest)
                        rtBest = time;
                    rtState = 0;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                rtTimer += Time.deltaTime;
            }
            else if (rtState == 3)
            {
                GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                GUI.Box(new Rect(ox, oy, w, h), "<size=22>Too early! Click to retry</size>");
                if (GUI.Button(new Rect(ox, oy, w, h), ""))
                {
                    rtState = 0;
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + h + 5f, w, 20f), "<size=11>Click when the screen turns green!</size>");
        }

        private void DrawTypingSpeed()
        {
            if (!tstActive && tstTotal == 0) TSNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Typing Speed</size>");
            GUI.Label(new Rect(170f, 48f, 400f, 20f), $"WPM: {tstWPM}  Accuracy: {(tstTotal > 0 ? (tstCorrect * 100 / tstTotal) : 100)}%  Time: {tstTimer:F1}s");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { TSNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float ox = 200f, oy = 80f, w = 400f;
            if (tstActive)
            {
                tstTimer += Time.deltaTime;
                GUIStyle ws = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
                GUI.Label(new Rect(ox, oy, w, 60f), tstCurrentWord, ws);
                GUI.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                GUI.SetNextControlName("TypingInput");
                tstTyped = GUI.TextField(new Rect(ox, oy + 70f, w, 30f), tstTyped, 50);
                if (tstTyped.Length >= tstCurrentWord.Length)
                {
                    if (tstTyped == tstCurrentWord) tstCorrect++;
                    tstTotal++;
                    int idx = UnityEngine.Random.Range(0, tstWords.Length);
                    tstCurrentWord = tstWords[idx];
                    tstTyped = "";
                    tstCorrect = (tstCorrect < 0 ? 0 : tstCorrect);
                }
                if (tstTimer >= 60f)
                {
                    tstWPM = tstCorrect;
                    tstActive = false;
                }
            }
            else if (tstTotal > 0)
            {
                tstWPM = (int)(tstCorrect / Mathf.Max(1f, tstTimer / 60f));
                GUIStyle fs = new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 40f, w, 40f), $"Final WPM: {tstWPM}", fs);
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + 120f, w, 20f), "<size=11>Type the word and press Enter. 60 second test.</size>");
        }
        private void TSNewGame()
        {
            tstWords = new string[] { "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", "gorilla", "tag", "monkey", "tree", "lobby", "room", "player", "bananas", "swing", "throw", "slide", "climb", "branch", "vine", "bounce", "grab", "hang", "kick", "punch", "wave", "dance", "run" };
            int idx = UnityEngine.Random.Range(0, tstWords.Length);
            tstCurrentWord = tstWords[idx];
            tstTyped = ""; tstCorrect = 0; tstTotal = 0; tstTimer = 0f; tstWPM = 0; tstActive = true; tstStartTime = Time.time;
        }

        private void DrawCatchObjects()
        {
            if (!coActive && coScore == 0) CONewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Catch Objects</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {coScore}  Lives: {coLives}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { CONewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            if (coActive)
            {
                coSpawnTimer += Time.deltaTime;
                coSpeed = 80f + coScore * 3f;
                float spawnInterval = Mathf.Max(0.4f, 1.5f - coScore * 0.02f);
                if (coSpawnTimer >= spawnInterval)
                {
                    coSpawnTimer = 0f;
                    coFalling.Add(new Vector3(UnityEngine.Random.Range(10f, coFieldW - 10f), -10f, 0f));
                }
                Event e = Event.current;
                if (e.type == EventType.MouseDown)
                    coBasketX = Mathf.Clamp(e.mousePosition.x - 200f, 0f, coFieldW - 40f);
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) coBasketX = Mathf.Max(0f, coBasketX - 20f);
                    if (e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) coBasketX = Mathf.Min(coFieldW - 40f, coBasketX + 20f);
                }
                for (int i = coFalling.Count - 1; i >= 0; i--)
                {
                    Vector3 obj = coFalling[i];
                    obj.y += coSpeed * Time.deltaTime;
                    coFalling[i] = obj;
                    if (obj.y >= coFieldH - 40f)
                    {
                        if (Mathf.Abs(obj.x - (coBasketX + 20f)) < 30f)
                        {
                            coScore += 10;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                        else
                        {
                            coLives--;
                            if (coLives <= 0) coActive = false;
                        }
                        coFalling.RemoveAt(i);
                    }
                }
            }
            GUI.backgroundColor = new Color(0.35f, 0.55f, 0.8f);
            GUI.Box(new Rect(200f, 80f + coFieldH - 35f, coFieldW, 35f), "");
            GUI.backgroundColor = new Color(0.8f, 0.6f, 0.2f);
            GUI.Box(new Rect(200f + coBasketX, 80f + coFieldH - 50f, 40f, 50f), "🧺");
            foreach (Vector3 obj in coFalling)
            {
                GUI.backgroundColor = Color.yellow;
                GUI.Box(new Rect(200f + obj.x - 8f, 80f + obj.y - 8f, 16f, 16f), "🍌");
            }
            GUI.backgroundColor = guiColorB;
            if (!coActive && coScore > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(200f, 80f + coFieldH / 2f - 15f, coFieldW, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(200f, 80f + coFieldH + 5f, coFieldW, 20f), "<size=11>Mouse or A/D to catch falling bananas!</size>");
        }
        private void CONewGame()
        {
            coBasketX = coFieldW / 2f - 20f;
            coFalling = new List<Vector3>();
            coScore = 0; coLives = 5; coActive = true;
            coSpawnTimer = 0f; coSpeed = 80f;
        }

        private void DrawPacman()
        {
            if (pacMaze == null) PACNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Pac-Man</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {pacScore}  Lives: {pacLives}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { PACNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            Event e = Event.current;
            if (e.type == EventType.KeyDown && pacActive)
            {
                if ((e.keyCode == KeyCode.UpArrow || e.keyCode == KeyCode.W) && PACCanMove(pacPR - 1, pacPC)) pacDir = 0;
                if ((e.keyCode == KeyCode.DownArrow || e.keyCode == KeyCode.S) && PACCanMove(pacPR + 1, pacPC)) pacDir = 1;
                if ((e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) && PACCanMove(pacPR, pacPC - 1)) pacDir = 2;
                if ((e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) && PACCanMove(pacPR, pacPC + 1)) pacDir = 3;
                e.Use();
            }
            if (pacActive)
            {
                pacMoveTimer += Time.deltaTime;
                float spd = 0.15f;
                if (pacMoveTimer >= spd)
                {
                    pacMoveTimer = 0f;
                    int dr = pacDir == 0 ? -1 : pacDir == 1 ? 1 : 0;
                    int dc = pacDir == 2 ? -1 : pacDir == 3 ? 1 : 0;
                    if (PACCanMove(pacPR + dr, pacPC + dc))
                    {
                        pacPR += dr;
                        pacPC += dc;
                        if (pacMaze[pacPR, pacPC] == 1)
                        {
                            pacMaze[pacPR, pacPC] = 0;
                            pacScore += 10;
                        }
                    }
                    pacGhostTimer += Time.deltaTime;
                    if (pacGhostTimer >= 0.3f)
                    {
                        pacGhostTimer = 0f;
                        for (int i = 0; i < pacGhosts.Count; i++)
                        {
                            int[] g = pacGhosts[i];
                            int gd = pacGhostDirs[i];
                            int[] dirs = { -1, 1, -10, 10 };
                            int nd = dirs[UnityEngine.Random.Range(0, 4)];
                            int nr = g[0] + (nd == -10 ? -1 : nd == 10 ? 1 : 0);
                            int nc = g[1] + (nd == -1 ? -1 : nd == 1 ? 1 : 0);
                            if (PACCanMove(nr, nc) && pacMaze[nr, nc] != 2)
                            {
                                pacGhosts[i] = new int[] { nr, nc };
                                pacGhostDirs[i] = nd;
                            }
                            if (pacGhosts[i][0] == pacPR && pacGhosts[i][1] == pacPC)
                            {
                                pacLives--;
                                pacPR = 1; pacPC = 1;
                                if (pacLives <= 0) pacActive = false;
                            }
                        }
                    }
                }
            }
            float cs = 24f, ox = 200f, oy = 72f;
            for (int r = 0; r < pacMaze.GetLength(0); r++)
            {
                for (int c = 0; c < pacMaze.GetLength(1); c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    if (pacMaze[r, c] == 2)
                    {
                        GUI.backgroundColor = new Color(0.2f, 0.3f, 0.8f);
                        GUI.Box(new Rect(x, y, cs - 1, cs - 1), "");
                    }
                    else if (pacMaze[r, c] == 1)
                    {
                        GUI.backgroundColor = new Color(0.3f, 0.3f, 0.35f);
                        GUI.Box(new Rect(x, y, cs - 1, cs - 1), "");
                        GUI.backgroundColor = Color.yellow;
                        GUI.Box(new Rect(x + 8f, y + 8f, 8f, 8f), "");
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
                        GUI.Box(new Rect(x, y, cs - 1, cs - 1), "");
                    }
                }
            }
            GUI.backgroundColor = Color.yellow;
            GUI.Box(new Rect(ox + pacPC * cs + 2f, oy + pacPR * cs + 2f, cs - 5f, cs - 5f), "●");
            foreach (int[] g in pacGhosts)
            {
                GUI.backgroundColor = new Color(0.9f, 0.2f, 0.2f);
                GUI.Box(new Rect(ox + g[1] * cs + 2f, oy + g[0] * cs + 2f, cs - 5f, cs - 5f), "👻");
            }
            GUI.backgroundColor = guiColorB;
            if (!pacActive && pacScore > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 12 * cs, pacMaze.GetLength(1) * cs, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + pacMaze.GetLength(0) * cs + 5f, pacMaze.GetLength(1) * cs, 20f), "<size=11>Arrows/WASD to move. Eat dots, avoid ghosts!</size>");
        }
        private void PACNewGame()
        {
            int rows = 12, cols = 15;
            pacMaze = new int[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    if (r == 0 || r == rows - 1 || c == 0 || c == cols - 1) pacMaze[r, c] = 2;
                    else pacMaze[r, c] = 1;
                }
            int[] wallR = { 3, 4, 5, 6, 7, 8, 9 };
            int[] wallC = { 7, 7, 3, 11, 5, 9, 7 };
            for (int i = 0; i < wallR.Length; i++)
                pacMaze[wallR[i], wallC[i]] = 2;
            pacMaze[1, 1] = 0;
            pacPR = 1; pacPC = 1; pacDir = 3; pacScore = 0; pacLives = 3;
            pacActive = true; pacMoveTimer = 0f; pacGhostTimer = 0f;
            pacGhosts = new List<int[]> { new int[] { rows - 2, cols - 2 }, new int[] { 1, cols - 2 } };
            pacGhostDirs = new List<int> { 2, 2 };
        }
        private bool PACCanMove(int r, int c)
        {
            if (r < 0 || r >= pacMaze.GetLength(0) || c < 0 || c >= pacMaze.GetLength(1)) return false;
            return pacMaze[r, c] != 2;
        }

        private void DrawTankBattle()
        {
            if (!tbActive && tbScore == 0) TBNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Tank Battle</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {tbScore}  Lives: {tbLives}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { TBNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float ox = 200f, oy = 72f, fw = 400f, fh = 300f;
            if (tbActive)
            {
                Event e = Event.current;
                if (e.type == EventType.MouseDown)
                {
                    Vector2 mp = e.mousePosition;
                    Vector2 target = new Vector2(mp.x - ox, mp.y - oy);
                    Vector2 dir = (target - new Vector2(tbPX, tbPY)).normalized;
                    if (tbShootCooldown <= 0f)
                    {
                        tbBullets.Add(new Vector3(tbPX, tbPY, 0f));
                        tbBulletPlayer.Add(true);
                        tbShootCooldown = 0.3f;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.W) tbPY = Mathf.Max(10f, tbPY - 15f);
                    if (e.keyCode == KeyCode.S) tbPY = Mathf.Min(fh - 10f, tbPY + 15f);
                    if (e.keyCode == KeyCode.A) tbPX = Mathf.Max(10f, tbPX - 15f);
                    if (e.keyCode == KeyCode.D) tbPX = Mathf.Min(fw - 10f, tbPX + 15f);
                    e.Use();
                }
                tbShootCooldown -= Time.deltaTime;
                tbSpawnTimer += Time.deltaTime;
                float spawnRate = Mathf.Max(0.5f, 2f - tbScore * 0.02f);
                if (tbSpawnTimer >= spawnRate)
                {
                    tbSpawnTimer = 0f;
                    float ex = UnityEngine.Random.Range(10f, fw - 10f);
                    tbEnemies.Add(new Vector4(ex, -10f, 0f, 1f));
                    tbEnemyHP.Add(1f);
                }
                for (int i = tbEnemies.Count - 1; i >= 0; i--)
                {
                    Vector4 en = tbEnemies[i];
                    en.y += 40f * Time.deltaTime;
                    tbEnemies[i] = en;
                    if (en.y > fh) { tbEnemies.RemoveAt(i); tbEnemyHP.RemoveAt(i); }
                    float dx = en.x - tbPX, dy = en.y - tbPY;
                    if (Mathf.Sqrt(dx * dx + dy * dy) < 15f)
                    {
                        tbLives--;
                        tbEnemies.RemoveAt(i);
                        tbEnemyHP.RemoveAt(i);
                        if (tbLives <= 0) tbActive = false;
                    }
                }
                for (int i = tbBullets.Count - 1; i >= 0; i--)
                {
                    Vector3 b = tbBullets[i];
                    bool isPlayer = tbBulletPlayer[i];
                    if (isPlayer)
                    {
                        b.y -= 300f * Time.deltaTime;
                        tbBullets[i] = b;
                        if (b.y < -10f) { tbBullets.RemoveAt(i); tbBulletPlayer.RemoveAt(i); continue; }
                        for (int j = tbEnemies.Count - 1; j >= 0; j--)
                        {
                            Vector4 en = tbEnemies[j];
                            float dx2 = b.x - en.x, dy2 = b.y - en.y;
                            if (Mathf.Sqrt(dx2 * dx2 + dy2 * dy2) < 12f)
                            {
                                float hp = tbEnemyHP[j] - 1f;
                                tbEnemyHP[j] = hp;
                                if (hp <= 0f)
                                {
                                    tbEnemies.RemoveAt(j);
                                    tbEnemyHP.RemoveAt(j);
                                    tbScore += 25;
                                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                }
                                tbBullets.RemoveAt(i);
                                tbBulletPlayer.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }
            }
            GUI.backgroundColor = new Color(0.15f, 0.18f, 0.12f);
            GUI.Box(new Rect(ox, oy, fw, fh), "");
            GUI.backgroundColor = Color.green;
            GUI.Box(new Rect(ox + tbPX - 8f, oy + tbPY - 8f, 16f, 16f), "▲");
            GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
            foreach (Vector4 en in tbEnemies)
            {
                if (en.y < 0f || en.y > fh) continue;
                GUI.Box(new Rect(ox + en.x - 8f, oy + en.y - 8f, 16f, 16f), "▼");
            }
            GUI.backgroundColor = Color.yellow;
            foreach (Vector3 b in tbBullets)
            {
                GUI.Box(new Rect(ox + b.x - 2f, oy + b.y - 6f, 4f, 12f), "");
            }
            GUI.backgroundColor = guiColorB;
            if (!tbActive && tbScore > 0)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f);
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + fh / 2f - 15f, fw, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + fh + 5f, fw, 20f), "<size=11>WASD to move, Click to shoot!</size>");
        }
        private void TBNewGame()
        {
            tbPX = 200f; tbPY = 280f;
            tbEnemies = new List<Vector4>();
            tbEnemyHP = new List<float>();
            tbBullets = new List<Vector3>();
            tbBulletPlayer = new List<bool>();
            tbScore = 0; tbLives = 5; tbActive = true;
            tbSpawnTimer = 0f; tbShootCooldown = 0f;
        }

        private void DrawBattleship()
        {
            if (bsPlayerBoard == null) BSNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Battleship</size>");
            if (bsPhase == 0) GUI.Label(new Rect(170f, 48f, 500f, 20f), "Place ships! Click to place, R to rotate");
            else if (bsPhase == 1) GUI.Label(new Rect(170f, 48f, 500f, 20f), "Fire! Click on enemy grid");
            else GUI.Label(new Rect(170f, 48f, 500f, 20f), bsGameOver ? (bsPlayerHits >= 17 ? "You Win!" : "You Lose!") : "");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { BSNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            if (bsPhase == 0)
            {
                if (GUI.Button(new Rect(520f, 46f, 70f, 22f), bsPlacingH ? "H→V" : "V→H"))
                { bsPlacingH = !bsPlacingH; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            }
            GUI.backgroundColor = guiColorB;
            float cs = 24f;
            string[] ships = { "2", "3", "3", "4", "5" };
            int[] shipSizes = { 2, 3, 3, 4, 5 };
            float pOx = 180f, eOx = 430f, oy = 72f;
            GUI.Label(new Rect(pOx, oy - 2f, cs * 10, 18f), "<size=12>Your Grid</size>");
            GUI.Label(new Rect(eOx, oy - 2f, cs * 10, 18f), "<size=12>Enemy Grid</size>");
            for (int r = 0; r < 10; r++)
            {
                for (int c = 0; c < 10; c++)
                {
                    float px = pOx + c * cs, py = oy + 18f + r * cs;
                    float ex = eOx + c * cs, ey = oy + 18f + r * cs;
                    bool pShip = bsPlayerShips[r, c];
                    bool pHits = bsPlayerBoard[r, c] == 2 || bsPlayerBoard[r, c] == 3;
                    GUI.backgroundColor = pHits ? (pShip ? new Color(0.9f, 0.3f, 0.3f) : new Color(0.3f, 0.5f, 0.9f)) : (pShip ? new Color(0.5f, 0.5f, 0.55f) : new Color(0.3f, 0.4f, 0.5f));
                    if (GUI.Button(new Rect(px, py, cs - 1, cs - 1), ""))
                    {
                        if (bsPhase == 0 && !pShip)
                        {
                            bsSelR = r; bsSelC = c;
                            if (BSPlaceShip(r, c, shipSizes[bsPlacingShip], bsPlacingH))
                            {
                                bsPlacingShip++;
                                if (bsPlacingShip >= 5) bsPhase = 1;
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                        }
                    }
                    bool eHit = bsEnemyBoard[r, c] == 2 || bsEnemyBoard[r, c] == 3;
                    bool eShip = bsEnemyShips[r, c];
                    GUI.backgroundColor = eHit ? (eShip ? new Color(0.9f, 0.2f, 0.2f) : new Color(0.3f, 0.5f, 0.9f)) : new Color(0.3f, 0.5f, 0.6f);
                    if (GUI.Button(new Rect(ex, ey, cs - 1, cs - 1), ""))
                    {
                        if (bsPhase == 1 && !bsGameOver && bsEnemyBoard[r, c] == 0)
                        {
                            bsEnemyBoard[r, c] = eShip ? 2 : 3;
                            if (eShip) { bsPlayerHits++; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                            else SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            int er = UnityEngine.Random.Range(0, 10), ec = UnityEngine.Random.Range(0, 10);
                            while (bsPlayerBoard[er, ec] != 0) { er = UnityEngine.Random.Range(0, 10); ec = UnityEngine.Random.Range(0, 10); }
                            bsPlayerBoard[er, ec] = bsPlayerShips[er, ec] ? 2 : 3;
                            if (bsPlayerShips[er, ec]) bsEnemyHits++;
                            if (bsPlayerHits >= 17 || bsEnemyHits >= 17) bsGameOver = true;
                        }
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
        }
        private void BSNewGame()
        {
            bsPlayerBoard = new int[10, 10];
            bsEnemyBoard = new int[10, 10];
            bsPlayerShips = new bool[10, 10];
            bsEnemyShips = new bool[10, 10];
            bsPhase = 0; bsSelR = bsSelC = -1;
            bsPlacingShip = 0; bsPlacingH = true;
            bsPlayerHits = bsEnemyHits = 0; bsGameOver = false;
            int[] shipSizes = { 2, 3, 3, 4, 5 };
            for (int i = 0; i < 5; i++)
            {
                bool placed = false;
                while (!placed)
                {
                    bool hor = UnityEngine.Random.value > 0.5f;
                    int r = UnityEngine.Random.Range(0, 10), c = UnityEngine.Random.Range(0, 10);
                    if (BSCanPlace(r, c, shipSizes[i], hor, bsEnemyShips))
                    {
                        for (int s = 0; s < shipSizes[i]; s++)
                        {
                            int rr = hor ? r : r + s;
                            int cc = hor ? c + s : c;
                            bsEnemyShips[rr, cc] = true;
                        }
                        placed = true;
                    }
                }
            }
        }
        private bool BSCanPlace(int r, int c, int size, bool hor, bool[,] board)
        {
            for (int s = 0; s < size; s++)
            {
                int rr = hor ? r : r + s;
                int cc = hor ? c + s : c;
                if (rr >= 10 || cc >= 10 || board[rr, cc]) return false;
            }
            return true;
        }
        private bool BSPlaceShip(int r, int c, int size, bool hor)
        {
            if (!BSCanPlace(r, c, size, hor, bsPlayerShips)) return false;
            for (int s = 0; s < size; s++)
            {
                int rr = hor ? r : r + s;
                int cc = hor ? c + s : c;
                bsPlayerShips[rr, cc] = true;
            }
            return true;
        }

        private void DrawYahtzee()
        {
            if (yzDice == null) YZNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Yahtzee</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Total: {yzTotal}  Rerolls: {yzRerolls}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { YZNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float ox = 200f, oy = 72f;
            string[] face = { "⚀", "⚁", "⚂", "⚃", "⚄", "⚅" };
            for (int i = 0; i < 5; i++)
            {
                GUI.backgroundColor = yzHeld[i] ? new Color(0.3f, 0.7f, 0.3f) : new Color(0.6f, 0.6f, 0.65f);
                if (GUI.Button(new Rect(ox + i * 50f, oy, 45f, 50f), yzDice[i] > 0 ? face[yzDice[i] - 1] : ""))
                {
                    yzHeld[i] = !yzHeld[i];
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorA;
            if (yzRerolls > 0 && !yzGameOver)
            {
                if (GUI.Button(new Rect(ox, oy + 58f, 120f, 25f), "Reroll Held"))
                {
                    yzRerolls--;
                    for (int i = 0; i < 5; i++)
                        if (!yzHeld[i]) yzDice[i] = UnityEngine.Random.Range(1, 7);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
                if (GUI.Button(new Rect(ox + 130f, oy + 58f, 120f, 25f), "Roll All"))
                {
                    yzRerolls--;
                    for (int i = 0; i < 5; i++)
                        if (!yzHeld[i]) yzDice[i] = UnityEngine.Random.Range(1, 7);
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorB;
            string[] cats = { "Ones", "Twos", "Threes", "Fours", "Fives", "Sixes", "3 of Kind", "4 of Kind", "Full House", "Sm Straight", "Lg Straight", "Yahtzee", "Chance" };
            float sy = oy + 90f;
            for (int i = 0; i < 13; i++)
            {
                if (yzUsed[i])
                {
                    GUI.backgroundColor = new Color(0.35f, 0.35f, 0.4f);
                    GUI.Label(new Rect(ox, sy + i * 20f, 120f, 19f), $"{cats[i]}: {yzScores[i]}");
                }
                else
                {
                    GUI.backgroundColor = guiColorA;
                    if (GUI.Button(new Rect(ox, sy + i * 20f, 120f, 19f), cats[i]))
                    {
                        yzScores[i] = YZCalcScore(i);
                        yzUsed[i] = true;
                        yzTotal += yzScores[i];
                        for (int d = 0; d < 5; d++) { yzDice[d] = UnityEngine.Random.Range(1, 7); yzHeld[d] = false; }
                        yzRerolls = 2;
                        if (Array.TrueForAll(yzUsed, u => u)) yzGameOver = true;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            if (yzGameOver)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.green;
                GUI.Label(new Rect(ox + 200f, oy, 200f, 30f), $"Final Score: {yzTotal}", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, sy + 13 * 20f + 10f, 300f, 20f), "<size=11>Click dice to hold, reroll, then score a category</size>");
        }
        private int YZCalcScore(int cat)
        {
            int[] counts = new int[7];
            int sum = 0;
            foreach (int d in yzDice) { counts[d]++; sum += d; }
            switch (cat)
            {
                case 0: return counts[1];
                case 1: return counts[2] * 2;
                case 2: return counts[3] * 3;
                case 3: return counts[4] * 4;
                case 4: return counts[5] * 5;
                case 5: return counts[6] * 6;
                case 6: for (int i = 1; i <= 6; i++) if (counts[i] >= 3) return sum; return 0;
                case 7: for (int i = 1; i <= 6; i++) if (counts[i] >= 4) return sum; return 0;
                case 8:
                    bool three = false, two = false;
                    for (int i = 1; i <= 6; i++) { if (counts[i] == 3) three = true; if (counts[i] == 2) two = true; }
                    return (three && two) ? 25 : 0;
                case 9:
                    bool hasSeq = false;
                    if (counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1) hasSeq = true;
                    if (counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1) hasSeq = true;
                    return hasSeq ? 30 : 0;
                case 10:
                    if (counts[1] >= 1 && counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1) return 40;
                    if (counts[2] >= 1 && counts[3] >= 1 && counts[4] >= 1 && counts[5] >= 1 && counts[6] >= 1) return 40;
                    return 0;
                case 11: return 50;
                case 12: return sum;
                default: return 0;
            }
        }
        private void YZNewGame()
        {
            yzDice = new int[5];
            yzHeld = new bool[5];
            yzScores = new int[13];
            yzUsed = new bool[13];
            yzRerolls = 2; yzTotal = 0; yzGameOver = false;
            for (int i = 0; i < 5; i++) { yzDice[i] = UnityEngine.Random.Range(1, 7); yzHeld[i] = false; }
        }

        private void DrawColorMatch()
        {
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Color Match</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {cmScore}  Time: {cmTimer:F1}s");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { CMNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float ox = 200f, oy = 72f;
            if (cmActive)
            {
                cmTimer -= Time.deltaTime;
                if (cmTimer <= 0f) cmActive = false;
                cmSliderR = GUI.HorizontalSlider(new Rect(ox, oy + 30f, 300f, 20f), cmSliderR, 0f, 1f);
                cmSliderG = GUI.HorizontalSlider(new Rect(ox, oy + 60f, 300f, 20f), cmSliderG, 0f, 1f);
                cmSliderB = GUI.HorizontalSlider(new Rect(ox, oy + 90f, 300f, 20f), cmSliderB, 0f, 1f);
                GUI.Label(new Rect(ox, oy + 15f, 300f, 15f), $"<size=12>R: {cmSliderR:F2}</size>");
                GUI.Label(new Rect(ox, oy + 45f, 300f, 15f), $"<size=12>G: {cmSliderG:F2}</size>");
                GUI.Label(new Rect(ox, oy + 75f, 300f, 15f), $"<size=12>B: {cmSliderB:F2}</size>");
                GUI.backgroundColor = cmPlayer;
                GUI.Box(new Rect(ox, oy + 120f, 140f, 80f), "");
                GUI.backgroundColor = cmTarget;
                GUI.Box(new Rect(ox + 160f, oy + 120f, 140f, 80f), "");
                GUI.backgroundColor = guiColorB;
                GUI.Label(new Rect(ox, oy + 210f, 140f, 18f), "<size=11>Your Color</size>");
                GUI.Label(new Rect(ox + 160f, oy + 210f, 140f, 18f), "<size=11>Target Color</size>");
                GUI.backgroundColor = guiColorA;
                cmPlayer = new Color(cmSliderR, cmSliderG, cmSliderB);
                if (GUI.Button(new Rect(ox + 100f, oy + 235f, 100f, 25f), "Submit"))
                {
                    float diff = Mathf.Abs(cmTarget.r - cmPlayer.r) + Mathf.Abs(cmTarget.g - cmPlayer.g) + Mathf.Abs(cmTarget.b - cmPlayer.b);
                    int pts = Mathf.Max(0, (int)((1f - diff / 3f) * 100));
                    cmScore += pts;
                    CMNewTarget();
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            else if (cmScore > 0)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(ox, oy + 100f, 300f, 30f), $"Final Score: {cmScore}", gs);
            }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(ox, oy + 270f, 300f, 20f), "<size=11>Match the target color with RGB sliders!</size>");
        }
        private void CMNewGame()
        {
            cmScore = 0; cmTimer = 60f; cmActive = true;
            cmSliderR = cmSliderG = cmSliderB = 0.5f;
            cmPlayer = new Color(0.5f, 0.5f, 0.5f);
            CMNewTarget();
        }
        private void CMNewTarget()
        {
            cmTarget = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        }

        private void DrawPipePuzzle()
        {
            if (ppGrid == null) PPNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Pipe Puzzle</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), ppSolved ? "Solved!" : "Click pipes to rotate them");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { PPNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 50f, ox = 200f, oy = 72f;
            string[] pipeChars = { "─", "│", "┌", "┐", "└", "┘", "┬", "┴", "├", "┤", "┼", "═", "║", "╔", "╗", "╚", "╝" };
            for (int r = 0; r < ppH; r++)
            {
                for (int c = 0; c < ppW; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    bool solved = ppSolved;
                    GUI.backgroundColor = solved ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.35f, 0.4f, 0.5f);
                    if (GUI.Button(new Rect(x, y, cs - 2, cs - 2), ""))
                    {
                        if (!ppSolved)
                        {
                            ppRotation[r, c] = (ppRotation[r, c] + 1) % 4;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            PPCheck();
                        }
                    }
                    int type = ppGrid[r, c];
                    GUI.color = Color.white;
                    GUIStyle ps = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter };
                    GUI.Label(new Rect(x, y + 5f, cs, cs), pipeChars[type % pipeChars.Length], ps);
                    GUI.color = Color.white;
                }
            }
            GUI.backgroundColor = guiColorB;
        }
        private void PPNewGame()
        {
            ppGrid = new int[ppH, ppW];
            ppRotation = new int[ppH, ppW];
            ppSolved = false;
            for (int r = 0; r < ppH; r++)
                for (int c = 0; c < ppW; c++)
                {
                    ppGrid[r, c] = UnityEngine.Random.Range(0, 5);
                    ppRotation[r, c] = 0;
                }
        }
        private void PPCheck()
        {
            bool allSame = true;
            for (int r = 0; r < ppH; r++)
                for (int c = 0; c < ppW; c++)
                    if (ppRotation[r, c] != 0) allSame = false;
            if (allSame) ppSolved = true;
        }

        private void DrawLightsOut()
        {
            if (loGrid == null) LONewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Lights Out</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Moves: {loMoves}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { LONewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 55f, ox = 220f, oy = 80f;
            bool allOff = true;
            for (int r = 0; r < loSize; r++)
                for (int c = 0; c < loSize; c++)
                    if (loGrid[r, c]) allOff = false;
            for (int r = 0; r < loSize; r++)
            {
                for (int c = 0; c < loSize; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    GUI.backgroundColor = loGrid[r, c] ? new Color(1f, 0.9f, 0.3f) : new Color(0.2f, 0.2f, 0.25f);
                    if (GUI.Button(new Rect(x, y, cs - 2, cs - 2), ""))
                    {
                        LOToggle(r, c);
                        loMoves++;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            if (allOff && loMoves > 0)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.green;
                GUI.Label(new Rect(ox, oy + loSize * cs + 10f, loSize * cs, 30f), $"Solved in {loMoves} moves!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(ox, oy + loSize * cs + 45f, loSize * cs, 20f), "<size=11>Click a light to toggle it and its neighbors</size>");
        }
        private void LONewGame()
        {
            loGrid = new bool[loSize, loSize];
            loMoves = 0;
            for (int i = 0; i < 8; i++)
                LOToggle(UnityEngine.Random.Range(0, loSize), UnityEngine.Random.Range(0, loSize));
        }
        private void LOToggle(int r, int c)
        {
            loGrid[r, c] = !loGrid[r, c];
            if (r > 0) loGrid[r - 1, c] = !loGrid[r - 1, c];
            if (r < loSize - 1) loGrid[r + 1, c] = !loGrid[r + 1, c];
            if (c > 0) loGrid[r, c - 1] = !loGrid[r, c - 1];
            if (c < loSize - 1) loGrid[r, c + 1] = !loGrid[r, c + 1];
        }

        private void DrawNonogram()
        {
            if (nnpGrid == null) NNNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Nonogram</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), nnpSolved ? "Solved!" : "Click to fill, Right-click to mark X");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(430f, 46f, 80f, 22f), "New Game"))
            { NNNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            float cs = 36f, ox = 250f, oy = 80f;
            Event e = Event.current;
            for (int r = 0; r < nnpH; r++)
            {
                if (r < nnpRowClues.Length && nnpRowClues[r] != null)
                {
                    string clue = string.Join(" ", nnpRowClues[r]);
                    GUI.Label(new Rect(ox - 60f, oy + r * cs, 58f, cs), $"<size=12>{clue}</size>");
                }
                for (int c = 0; c < nnpW; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    if (r == 0 && c < nnpColClues.Length && nnpColClues[c] != null)
                    {
                        string clue = string.Join("\n", nnpColClues[c]);
                        GUI.Label(new Rect(x, oy - 40f, cs, 38f), $"<size=11>{clue}</size>");
                    }
                    if (nnpGrid[r, c] == 1)
                        GUI.backgroundColor = new Color(0.2f, 0.2f, 0.25f);
                    else if (nnpGrid[r, c] == 2)
                        GUI.backgroundColor = new Color(0.4f, 0.4f, 0.45f);
                    else
                        GUI.backgroundColor = new Color(0.75f, 0.75f, 0.8f);
                    if (GUI.Button(new Rect(x, y, cs - 2, cs - 2), nnpGrid[r, c] == 2 ? "×" : ""))
                    {
                        if (!nnpSolved)
                        {
                            if (e.button == 0)
                                nnpGrid[r, c] = nnpGrid[r, c] == 1 ? 0 : 1;
                            else
                                nnpGrid[r, c] = nnpGrid[r, c] == 2 ? 0 : 2;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            NNCheck();
                        }
                    }
                }
            }
            GUI.backgroundColor = guiColorB;
            if (nnpSolved)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.green;
                GUI.Label(new Rect(ox, oy + nnpH * cs + 10f, nnpW * cs, 30f), "Congratulations!", gs);
                GUI.color = Color.white;
            }
        }
        private void NNNewGame()
        {
            nnpGrid = new int[nnpH, nnpW];
            nnpSolution = new int[nnpH, nnpW];
            nnpSolved = false;
            for (int r = 0; r < nnpH; r++)
                for (int c = 0; c < nnpW; c++)
                {
                    nnpSolution[r, c] = UnityEngine.Random.Range(0, 2);
                    nnpGrid[r, c] = 0;
                }
            nnpRowClues = new List<int>[nnpH];
            nnpColClues = new List<int>[nnpW];
            for (int r = 0; r < nnpH; r++)
            {
                nnpRowClues[r] = new List<int>();
                int count = 0;
                for (int c = 0; c < nnpW; c++)
                {
                    if (nnpSolution[r, c] == 1) count++;
                    else if (count > 0) { nnpRowClues[r].Add(count); count = 0; }
                }
                if (count > 0) nnpRowClues[r].Add(count);
                if (nnpRowClues[r].Count == 0) nnpRowClues[r].Add(0);
            }
            for (int c = 0; c < nnpW; c++)
            {
                nnpColClues[c] = new List<int>();
                int count = 0;
                for (int r = 0; r < nnpH; r++)
                {
                    if (nnpSolution[r, c] == 1) count++;
                    else if (count > 0) { nnpColClues[c].Add(count); count = 0; }
                }
                if (count > 0) nnpColClues[c].Add(count);
                if (nnpColClues[c].Count == 0) nnpColClues[c].Add(0);
            }
        }
        private void NNCheck()
        {
            for (int r = 0; r < nnpH; r++)
                for (int c = 0; c < nnpW; c++)
                    if ((nnpGrid[r, c] == 1 ? 1 : 0) != nnpSolution[r, c]) return;
            nnpSolved = true;
        }

        private void ResetTT()
        {
            for (int i = 0; i < 9; i++) ttb[i] = "";
            ttWinner = 0;
            ttTurn = 0;
            ttLineA = -1;
            ttLineB = -1;
            ttAISym = ttPlayerIsX ? "O" : "X";
            ttPlayerSym = ttPlayerIsX ? "X" : "O";
        }

        private bool ttFull()
        {
            for (int i = 0; i < 9; i++)
                if (ttb[i] == "") return false;
            return true;
        }

        private void CheckTTWin()
        {
            int[,] lines = { {0,1,2},{3,4,5},{6,7,8},{0,3,6},{1,4,7},{2,5,8},{0,4,8},{2,4,6} };
            for (int l = 0; l < 8; l++)
            {
                int a = lines[l,0], b = lines[l,1], c = lines[l,2];
                if (ttb[a] != "" && ttb[a] == ttb[b] && ttb[b] == ttb[c])
                {
                    ttWinner = ttb[a] == "X" ? 1 : 2;
                    ttLineA = a;
                    ttLineB = c;
                    bool playerWon = (ttWinner == 1 && ttPlayerIsX) || (ttWinner == 2 && !ttPlayerIsX);
                    if (playerWon) ttScoreX++;
                    else ttScoreO++;
                    return;
                }
            }
            if (ttFull())
            {
                ttWinner = 3;
                ttScoreD++;
            }
        }

        private void TTAIMove()
        {
            if (ttWinner != 0) return;

            int move = -1;
            if (ttDiff == 0)
            {
                List<int> empty = new List<int>();
                for (int i = 0; i < 9; i++)
                    if (ttb[i] == "") empty.Add(i);
                if (empty.Count > 0) move = empty[UnityEngine.Random.Range(0, empty.Count)];
            }
            else if (ttDiff == 1)
            {
                move = TTMediumAI();
            }
            else
            {
                move = TTHardAI();
            }

            if (move >= 0 && move < 9 && ttb[move] == "")
            {
                ttb[move] = ttAISym;
                ttTurn = 0;
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                CheckTTWin();
            }
        }

        private int TTMediumAI()
        {
            if (UnityEngine.Random.Range(0, 3) == 0)
            {
                List<int> empty = new List<int>();
                for (int i = 0; i < 9; i++)
                    if (ttb[i] == "") empty.Add(i);
                return empty.Count > 0 ? empty[UnityEngine.Random.Range(0, empty.Count)] : -1;
            }
            for (int i = 0; i < 9; i++)
            {
                if (ttb[i] == "")
                {
                    ttb[i] = ttAISym;
                    bool win = TTCheckLine(ttAISym);
                    ttb[i] = "";
                    if (win) return i;
                }
            }
            for (int i = 0; i < 9; i++)
            {
                if (ttb[i] == "")
                {
                    ttb[i] = ttPlayerSym;
                    bool block = TTCheckLine(ttPlayerSym);
                    ttb[i] = "";
                    if (block) return i;
                }
            }
            List<int> rest = new List<int>();
            for (int i = 0; i < 9; i++)
                if (ttb[i] == "") rest.Add(i);
            return rest.Count > 0 ? rest[UnityEngine.Random.Range(0, rest.Count)] : -1;
        }

        private bool TTCheckLine(string player)
        {
            int[,] lines = { {0,1,2},{3,4,5},{6,7,8},{0,3,6},{1,4,7},{2,5,8},{0,4,8},{2,4,6} };
            for (int l = 0; l < 8; l++)
            {
                int a = lines[l,0], b = lines[l,1], c = lines[l,2];
                if (ttb[a] == player && ttb[b] == player && ttb[c] == player)
                    return true;
            }
            return false;
        }

        private int TTHardAI()
        {
            for (int i = 0; i < 9; i++)
            {
                if (ttb[i] == "")
                {
                    ttb[i] = ttAISym;
                    bool win = TTCheckLine(ttAISym);
                    ttb[i] = "";
                    if (win) return i;
                }
            }
            for (int i = 0; i < 9; i++)
            {
                if (ttb[i] == "")
                {
                    ttb[i] = ttPlayerSym;
                    bool block = TTCheckLine(ttPlayerSym);
                    ttb[i] = "";
                    if (block) return i;
                }
            }
            int bestScore = -100;
            int bestMove = -1;
            for (int i = 0; i < 9; i++)
            {
                if (ttb[i] == "")
                {
                    ttb[i] = ttAISym;
                    int score = -TTMiniMaxHelper(false, -100, 100);
                    ttb[i] = "";
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = i;
                    }
                }
            }
            return bestMove;
        }

        private int TTMiniMaxHelper(bool isMax, int alpha, int beta)
        {
            if (TTCheckLine(ttAISym)) return 1;
            if (TTCheckLine(ttPlayerSym)) return -1;
            if (ttFull()) return 0;

            if (isMax)
            {
                int best = -100;
                for (int i = 0; i < 9; i++)
                {
                    if (ttb[i] == "")
                    {
                        ttb[i] = ttAISym;
                        int s = TTMiniMaxHelper(false, alpha, beta);
                        ttb[i] = "";
                        best = Math.Max(best, s);
                        alpha = Math.Max(alpha, s);
                        if (beta <= alpha) break;
                    }
                }
                return best;
            }
            else
            {
                int best = 100;
                for (int i = 0; i < 9; i++)
                {
                    if (ttb[i] == "")
                    {
                        ttb[i] = ttPlayerSym;
                        int s = TTMiniMaxHelper(true, alpha, beta);
                        ttb[i] = "";
                        best = Math.Min(best, s);
                        beta = Math.Min(beta, s);
                        if (beta <= alpha) break;
                    }
                }
                return best;
            }
        }

        private float wasdSpeed = 1f;
        private float wasdRotation = 1f;
        private float wasdJump = 1f;

        public static Color guiBgColor = Color.white;
        public static Color guiContentColor = Color.blue;
        public static Color guiColorA = new Color(1f, 0f, 1f);
        public static Color guiColorB = new Color(0.54f, 0.17f, 0.89f);
        public static Color guiIconColor = Color.white;
        private static bool isRainbowTheme;
        private static float rainbowTime;
        private static Texture2D menuIconTexture;
        private static GUIStyle titleStyle;
        private static Texture2D gradientTexture;

        private static Texture2D GetGradientTexture(Color top, Color bottom)
        {
            if (gradientTexture != null) return gradientTexture;
            gradientTexture = new Texture2D(1, 32);
            gradientTexture.hideFlags = HideFlags.HideAndDontSave;
            for (int i = 0; i < 32; i++)
                gradientTexture.SetPixel(0, i, Color.Lerp(top, bottom, i / 31f));
            gradientTexture.Apply();
            return gradientTexture;
        }

        private static Texture2D GetMenuIcon()
        {
            if (menuIconTexture == null)
                menuIconTexture = LoadTextureFromResource($"{PluginInfo.ClientResourcePath}.icon.png");
            return menuIconTexture;
        }

        // ==================== ROCK PAPER SCISSORS ====================
        private void DrawRockPaperScissors()
        {
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Rock Paper Scissors</size>");
            GUI.Label(new Rect(170f, 48f, 400f, 20f), $"Wins: {rpsWins}  Losses: {rpsLosses}  Draws: {rpsDraws}  Streak: {rpsStreak}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "Reset"))
            { rpsWins = 0; rpsLosses = 0; rpsDraws = 0; rpsStreak = 0; rpsResult = ""; rpsPlayerChoice = -1; rpsAIChoice = -1; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            string[] choices = { "Rock", "Paper", "Scissors" };
            Color[] choiceColors = { new Color(0.7f, 0.7f, 0.7f), new Color(0.3f, 0.6f, 0.9f), new Color(0.9f, 0.5f, 0.2f) };
            for (int i = 0; i < 3; i++)
            {
                GUI.backgroundColor = rpsPlayerChoice == i ? choiceColors[i] : guiColorB;
                if (GUI.Button(new Rect(220f + i * 100f, 90f, 90f, 40f), choices[i]))
                {
                    rpsPlayerChoice = i;
                    rpsAIChoice = UnityEngine.Random.Range(0, 3);
                    if (rpsPlayerChoice == rpsAIChoice) { rpsResult = "Draw!"; rpsDraws++; rpsStreak = 0; }
                    else if ((rpsPlayerChoice == 0 && rpsAIChoice == 2) || (rpsPlayerChoice == 1 && rpsAIChoice == 0) || (rpsPlayerChoice == 2 && rpsAIChoice == 1))
                    { rpsResult = "You Win!"; rpsWins++; rpsStreak = rpsStreak > 0 ? rpsStreak + 1 : 1; }
                    else { rpsResult = "AI Wins!"; rpsLosses++; rpsStreak = rpsStreak < 0 ? rpsStreak - 1 : -1; }
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorB;

            if (rpsPlayerChoice >= 0 && rpsAIChoice >= 0)
            {
                GUI.Label(new Rect(170f, 150f, 200f, 25f), $"<size=14>You: {choices[rpsPlayerChoice]}  vs  AI: {choices[rpsAIChoice]}</size>");
                GUIStyle bigResult = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = rpsResult == "You Win!" ? Color.green : rpsResult == "AI Wins!" ? Color.red : Color.yellow;
                GUI.Label(new Rect(200f, 190f, 230f, 40f), rpsResult, bigResult);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 250f, 400f, 20f), "<size=11>Pick Rock, Paper, or Scissors to play!</size>");
        }

        // ==================== NUMBER GUESS ====================
        private void DrawNumberGuess()
        {
            if (ngTarget == 0) { ngTarget = UnityEngine.Random.Range(1, 101); ngAttempts = 0; ngWon = false; ngHint = ""; ngInput = ""; }
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Number Guess</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Attempts: {ngAttempts}  Best: {(ngBest < 0 ? "-" : ngBest.ToString())}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Number"))
            { ngTarget = UnityEngine.Random.Range(1, 101); ngAttempts = 0; ngWon = false; ngHint = ""; ngInput = ""; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            GUI.Label(new Rect(200f, 90f, 300f, 25f), "<size=14>Guess a number between 1 and 100:</size>");
            ngInput = GUI.TextField(new Rect(200f, 120f, 150f, 25f), ngInput);
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(360f, 120f, 60f, 25f), "Guess") && !ngWon)
            {
                if (int.TryParse(ngInput, out int g) && g >= 1 && g <= 100)
                {
                    ngGuess = g;
                    ngAttempts++;
                    if (g == ngTarget)
                    {
                        ngWon = true;
                        ngHint = "Correct!";
                        if (ngBest < 0 || ngAttempts < ngBest) ngBest = ngAttempts;
                    }
                    else if (g < ngTarget) ngHint = "Too low!";
                    else ngHint = "Too high!";
                    ngInput = "";
                    SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                }
            }
            GUI.backgroundColor = guiColorB;

            GUIStyle hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = ngWon ? Color.green : Color.yellow;
            GUI.Label(new Rect(200f, 160f, 250f, 30f), ngHint, hintStyle);
            GUI.color = Color.white;

            if (ngWon)
            {
                GUIStyle winStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
                GUI.Label(new Rect(200f, 200f, 250f, 25f), $"Got it in {ngAttempts} attempts!", winStyle);
            }
            GUI.Label(new Rect(170f, 260f, 400f, 20f), "<size=11>Type a number and click Guess</size>");
        }

        // ==================== DICE ROLL ====================
        private void DrawDiceRoll()
        {
            if (drDice[0] == 0 && drDice[1] == 0) { DRNewGame(); }
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Dice Roll</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {drScore}  Best: {drBestScore}  Rerolls: {drRerolls}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { DRNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            for (int i = 0; i < 5; i++)
            {
                GUI.backgroundColor = drHeld[i] ? new Color(0.8f, 0.6f, 0.1f) : guiColorB;
                if (GUI.Button(new Rect(200f + i * 60f, 90f, 55f, 55f), drDice[i].ToString()))
                {
                    if (drRolled) { drHeld[i] = !drHeld[i]; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                }
            }
            GUI.backgroundColor = guiColorB;

            GUI.Label(new Rect(170f, 155f, 300f, 20f), "<size=11>Click dice to hold/unhold them</size>");

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(220f, 185f, 100f, 30f), drRerolls < 3 ? "Roll" : "Score"))
            {
                if (drRerolls < 3)
                {
                    for (int i = 0; i < 5; i++)
                        if (!drHeld[i]) drDice[i] = UnityEngine.Random.Range(1, 7);
                    drRerolls++;
                    drRolled = true;
                }
                else
                {
                    drScore += DRCalculateScore();
                    if (drScore > drBestScore) drBestScore = drScore;
                    DRNewGame();
                }
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            GUI.Label(new Rect(170f, 230f, 400f, 20f), $"<size=11>Turn Score: {DRCalculateScore()}  |  Roll up to 3 times per turn</size>");
        }
        private void DRNewGame()
        {
            for (int i = 0; i < 5; i++) { drDice[i] = UnityEngine.Random.Range(1, 7); drHeld[i] = false; }
            drRerolls = 0; drRolled = false;
        }
        private int DRCalculateScore()
        {
            int[] counts = new int[7];
            for (int i = 0; i < 5; i++) counts[drDice[i]]++;
            int score = 0;
            if (counts.Contains(5)) score = 50;
            else if (counts.Contains(4)) score = 40;
            else if (counts.Contains(3) && counts.Contains(2)) score = 25;
            else if (counts.Contains(3)) score = 15;
            else if (counts.Where(c => c >= 2).Count() >= 2) score = 10;
            else { for (int i = 1; i <= 6; i++) score += counts[i] * i; }
            return score;
        }

        // ==================== COIN FLIP ====================
        private void DrawCoinFlip()
        {
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Coin Flip</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Total Flips: {cfTotal}  |  Streak: {Mathf.Abs(cfStreak)} {cfStreakType}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "Reset"))
            { cfResult = -1; cfStreak = 0; cfTotal = 0; cfStreakType = ""; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            GUI.backgroundColor = new Color(0.9f, 0.8f, 0.2f);
            if (GUI.Button(new Rect(250f, 100f, 120f, 120f), cfResult < 0 ? "Flip!" : (cfResult == 0 ? "HEADS" : "TAILS")))
            {
                cfResult = UnityEngine.Random.Range(0, 2);
                cfTotal++;
                string type = cfResult == 0 ? "Heads" : "Tails";
                if (cfStreakType == type || cfStreak == 0)
                { cfStreak = cfStreak >= 0 ? cfStreak + 1 : 1; cfStreakType = type; }
                else { cfStreak = cfStreak > 0 ? -1 : 1; cfStreakType = type; }
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            GUI.backgroundColor = guiColorB;

            GUIStyle bigStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            GUI.color = cfStreakType == "Heads" ? new Color(1f, 0.85f, 0.2f) : new Color(0.7f, 0.7f, 0.7f);
            GUI.Label(new Rect(200f, 240f, 230f, 30f), cfResult >= 0 ? (cfStreakType + "!") : "", bigStyle);
            GUI.color = Color.white;

            GUIStyle streakStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter };
            GUI.Label(new Rect(200f, 280f, 230f, 25f), Mathf.Abs(cfStreak) > 1 ? $"Streak of {Mathf.Abs(cfStreak)}!" : "", streakStyle);
            GUI.Label(new Rect(170f, 330f, 400f, 20f), "<size=11>Click the coin to flip it</size>");
        }

        // ==================== BLACKJACK ====================
        private void DrawBlackjack()
        {
            if (bjPlayerHand.Count == 0) { bjChips = 1000; bjBetting = true; bjBet = 100; }
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Blackjack</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Chips: {bjChips}  |  Bet: {bjBet}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Hand"))
            { bjBetting = true; bjGameOver = false; bjResult = ""; bjPlayerHand.Clear(); bjDealerHand.Clear(); bjPlayerLabels.Clear(); bjDealerLabels.Clear(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            if (bjBetting)
            {
                GUI.Label(new Rect(200f, 90f, 200f, 25f), "<size=14>Place your bet:</size>");
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(200f, 120f, 60f, 25f), "-50")) { bjBet = Mathf.Max(50, bjBet - 50); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                GUI.backgroundColor = guiColorB;
                GUI.Label(new Rect(270f, 120f, 80f, 25f), bjBet.ToString());
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(360f, 120f, 60f, 25f), "+50")) { bjBet = Mathf.Min(bjChips, bjBet + 50); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
                GUI.backgroundColor = guiColorB;
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(260f, 160f, 100f, 30f), "Deal"))
                {
                    if (bjBet > 0 && bjBet <= bjChips)
                    {
                        bjChips -= bjBet;
                        bjPlayerHand.Clear(); bjDealerHand.Clear();
                        bjPlayerLabels.Clear(); bjDealerLabels.Clear();
                        bjPlayerHand.Add(BJDrawCard()); bjPlayerHand.Add(BJDrawCard());
                        bjDealerHand.Add(BJDrawCard()); bjDealerHand.Add(BJDrawCard());
                        for (int i = 0; i < bjPlayerHand.Count; i++) bjPlayerLabels.Add(BJCardName(bjPlayerHand[i]));
                        for (int i = 0; i < bjDealerHand.Count; i++) bjDealerLabels.Add(BJCardName(bjDealerHand[i]));
                        bjDealerHidden = true; bjGameOver = false; bjBetting = false; bjResult = "";
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
                GUI.backgroundColor = guiColorB;
                GUI.Label(new Rect(200f, 200f, 300f, 20f), "<size=11>Min bet: 50 chips</size>");
            }
            else
            {
                GUIStyle cardStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, richText = true };
                int dealerFirstVal = bjDealerHand[0] == 1 ? 11 : (bjDealerHand[0] >= 10 ? 10 : bjDealerHand[0]);
                GUI.Label(new Rect(170f, 75f, 200f, 20f), $"<size=12>Dealer ({(bjDealerHidden ? dealerFirstVal + " + ?" : BJHandValue(bjDealerHand).ToString())})</size>");
                for (int i = 0; i < bjDealerHand.Count; i++)
                {
                    GUI.backgroundColor = new Color(0.1f, 0.5f, 0.1f);
                    if (i == 1 && bjDealerHidden)
                        GUI.Box(new Rect(180f + i * 50f, 100f, 45f, 60f), "?");
                    else
                    {
                        GUI.backgroundColor = new Color(0.95f, 0.95f, 0.9f);
                        GUI.Box(new Rect(180f + i * 50f, 100f, 45f, 60f), bjDealerLabels[i]);
                    }
                }

                GUI.backgroundColor = guiColorB;
                GUI.Label(new Rect(170f, 175f, 200f, 20f), $"<size=12>Your Hand ({BJHandValue(bjPlayerHand)})</size>");
                for (int i = 0; i < bjPlayerHand.Count; i++)
                {
                    GUI.backgroundColor = new Color(0.95f, 0.95f, 0.9f);
                    GUI.Box(new Rect(180f + i * 50f, 200f, 45f, 60f), bjPlayerLabels[i]);
                }

                if (!bjGameOver)
                {
                    GUI.backgroundColor = guiColorA;
                    if (GUI.Button(new Rect(200f, 280f, 80f, 30f), "Hit"))
                    {
                        bjPlayerHand.Add(BJDrawCard());
                        bjPlayerLabels.Add(BJCardName(bjPlayerHand[bjPlayerHand.Count - 1]));
                        if (BJHandValue(bjPlayerHand) > 21) { bjResult = "Bust! You lose."; bjGameOver = true; }
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = new Color(0.8f, 0.3f, 0.3f);
                    if (GUI.Button(new Rect(310f, 280f, 80f, 30f), "Stand"))
                    {
                        bjDealerHidden = false;
                        while (BJHandValue(bjDealerHand) < 17)
                        { bjDealerHand.Add(BJDrawCard()); bjDealerLabels.Add(BJCardName(bjDealerHand[bjDealerHand.Count - 1])); }
                        int dv = BJHandValue(bjDealerHand);
                        int pv = BJHandValue(bjPlayerHand);
                        if (dv > 21) { bjResult = "Dealer busts! You win!"; bjChips += bjBet * 2; }
                        else if (pv > dv) { bjResult = "You win!"; bjChips += bjBet * 2; }
                        else if (pv < dv) { bjResult = "Dealer wins."; }
                        else { bjResult = "Push!"; bjChips += bjBet; }
                        bjGameOver = true;
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                    GUI.backgroundColor = guiColorB;
                }

                if (bjGameOver)
                {
                    GUIStyle resultStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                    GUI.color = bjResult.Contains("win") || bjResult.Contains("Win") ? Color.green : Color.red;
                    GUI.Label(new Rect(200f, 325f, 200f, 25f), bjResult, resultStyle);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(200f, 355f, 200f, 20f), $"Chips: {bjChips}");
                }
            }
            GUI.Label(new Rect(170f, 390f, 400f, 20f), "<size=11>Get as close to 21 as possible without going over!</size>");
        }
        private int BJDrawCard() { return UnityEngine.Random.Range(1, 14); }
        private int BJHandValue(List<int> hand)
        {
            int val = 0, aces = 0;
            foreach (int c in hand) { if (c == 1) { aces++; val += 11; } else if (c >= 10) val += 10; else val += c; }
            while (val > 21 && aces > 0) { val -= 10; aces--; }
            return val;
        }
        private string BJCardName(int c)
        {
            string[] names = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
            return names[Mathf.Clamp(c, 1, 13) - 1];
        }

        // ==================== GOMOKU ====================
        private void DrawGomoku()
        {
            if (gmBoard == null) GMNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Gomoku (Five in a Row)</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), gmWinner == 0 ? (gmTurn == 0 ? "Your turn" : "AI thinking...") : (gmWinner == 1 ? "You win!" : "AI wins!"));
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { GMNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 20f, ox = 190f, oy = 75f;
            for (int r = 0; r < 15; r++)
                for (int c = 0; c < 15; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    GUI.backgroundColor = gmBoard[r, c] == 0 ? new Color(0.35f, 0.25f, 0.15f) : gmBoard[r, c] == 1 ? Color.white : Color.black;
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (gmBoard[r, c] == 0 && gmTurn == 0 && gmWinner == 0 && !gmAIThinking)
                        {
                            gmBoard[r, c] = 1;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            if (GMCheckWin(1)) { gmWinner = 1; }
                            else if (GMFull()) { gmWinner = 3; }
                            else { gmTurn = 1; gmAIThinking = true; Invoke(nameof(GMAIMove), 0.2f); }
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;
        }
        private void GMNewGame()
        {
            gmBoard = new int[15, 15];
            gmTurn = 0; gmWinner = 0; gmAIThinking = false;
            gmWinR1 = gmWinC1 = gmWinR2 = gmWinC2 = -1;
        }
        private bool GMCheckWin(int p)
        {
            for (int r = 0; r < 15; r++)
                for (int c = 0; c < 11; c++)
                    if (gmBoard[r, c] == p && gmBoard[r, c + 1] == p && gmBoard[r, c + 2] == p && gmBoard[r, c + 3] == p && gmBoard[r, c + 4] == p) return true;
            for (int r = 0; r < 11; r++)
                for (int c = 0; c < 15; c++)
                    if (gmBoard[r, c] == p && gmBoard[r + 1, c] == p && gmBoard[r + 2, c] == p && gmBoard[r + 3, c] == p && gmBoard[r + 4, c] == p) return true;
            for (int r = 0; r < 11; r++)
                for (int c = 0; c < 11; c++)
                    if (gmBoard[r, c] == p && gmBoard[r + 1, c + 1] == p && gmBoard[r + 2, c + 2] == p && gmBoard[r + 3, c + 3] == p && gmBoard[r + 4, c + 4] == p) return true;
            for (int r = 4; r < 15; r++)
                for (int c = 0; c < 11; c++)
                    if (gmBoard[r, c] == p && gmBoard[r - 1, c + 1] == p && gmBoard[r - 2, c + 2] == p && gmBoard[r - 3, c + 3] == p && gmBoard[r - 4, c + 4] == p) return true;
            return false;
        }
        private bool GMFull()
        {
            for (int r = 0; r < 15; r++)
                for (int c = 0; c < 15; c++)
                    if (gmBoard[r, c] == 0) return false;
            return true;
        }
        private void GMAIMove()
        {
            gmAIThinking = false;
            if (gmWinner != 0) return;
            int bestR = -1, bestC = -1, bestScore = -1;
            for (int r = 0; r < 15; r++)
                for (int c = 0; c < 15; c++)
                    if (gmBoard[r, c] == 0)
                    {
                        int sc = GMEval(r, c, 2) + GMEval(r, c, 1) / 2;
                        if (sc > bestScore) { bestScore = sc; bestR = r; bestC = c; }
                    }
            if (bestR >= 0)
            {
                gmBoard[bestR, bestC] = 2;
                if (GMCheckWin(2)) gmWinner = 2;
                else if (GMFull()) gmWinner = 3;
                else gmTurn = 0;
            }
        }
        private int GMEval(int r, int c, int p)
        {
            int score = 0;
            int[][] dirs = { new[]{0,1}, new[]{1,0}, new[]{1,1}, new[]{-1,1} };
            foreach (var d in dirs)
            {
                int cnt = 0;
                for (int i = 1; i <= 4; i++)
                {
                    int nr = r + d[0] * i, nc = c + d[1] * i;
                    if (nr >= 0 && nr < 15 && nc >= 0 && nc < 15 && gmBoard[nr, nc] == p) cnt++;
                    else break;
                }
                for (int i = 1; i <= 4; i++)
                {
                    int nr = r - d[0] * i, nc = c - d[1] * i;
                    if (nr >= 0 && nr < 15 && nc >= 0 && nc < 15 && gmBoard[nr, nc] == p) cnt++;
                    else break;
                }
                if (cnt >= 4) score += 10000;
                else if (cnt == 3) score += 1000;
                else if (cnt == 2) score += 100;
                else if (cnt == 1) score += 10;
            }
            return score;
        }

        // ==================== DOTS AND BOXES ====================
        private void DrawDotsAndBoxes()
        {
            if (dbHoriz == null) DBNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Dots and Boxes</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"P1: {dbScore1}  |  P2: {dbScore2}  |  {(dbTurn == 0 ? "P1's turn" : "P2's turn")}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { DBNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 40f, ox = 220f, oy = 85f;
            for (int r = 0; r <= dbRows; r++)
                for (int c = 0; c <= dbCols; c++)
                {
                    GUI.backgroundColor = new Color(0.8f, 0.7f, 0.3f);
                    GUI.Box(new Rect(ox + c * cs - 4, oy + r * cs - 4, 8, 8), "");
                }
            for (int r = 0; r < dbRows; r++)
                for (int c = 0; c < dbCols; c++)
                {
                    if (dbBoxes[r, c] > 0)
                    {
                        GUI.backgroundColor = dbBoxes[r, c] == 1 ? new Color(0.3f, 0.5f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f, 0.3f);
                        GUI.Box(new Rect(ox + c * cs + 6, oy + r * cs + 6, cs - 12, cs - 12), dbBoxes[r, c] == 1 ? "P1" : "P2");
                    }
                }
            for (int r = 0; r <= dbRows; r++)
                for (int c = 0; c < dbCols; c++)
                {
                    bool drawn = dbHoriz[r, c];
                    GUI.backgroundColor = drawn ? (dbTurn == 0 ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f)) : new Color(0.3f, 0.3f, 0.35f);
                    if (GUI.Button(new Rect(ox + c * cs + 8, oy + r * cs - 3, cs - 16, 6), drawn ? "" : ""))
                    {
                        if (!drawn && !dbGameOver)
                        {
                            dbHoriz[r, c] = true;
                            bool scored = DBCheckBox();
                            if (!scored) dbTurn = 1 - dbTurn;
                            DBCheckDone();
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                }
            for (int r = 0; r < dbRows; r++)
                for (int c = 0; c <= dbCols; c++)
                {
                    bool drawn = dbVert[r, c];
                    GUI.backgroundColor = drawn ? (dbTurn == 0 ? new Color(0.3f, 0.5f, 1f) : new Color(1f, 0.3f, 0.3f)) : new Color(0.3f, 0.3f, 0.35f);
                    if (GUI.Button(new Rect(ox + c * cs - 3, oy + r * cs + 8, 6, cs - 16), drawn ? "" : ""))
                    {
                        if (!drawn && !dbGameOver)
                        {
                            dbVert[r, c] = true;
                            bool scored = DBCheckBox();
                            if (!scored) dbTurn = 1 - dbTurn;
                            DBCheckDone();
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;
            if (dbGameOver)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                string winner = dbScore1 > dbScore2 ? "P1 wins!" : dbScore2 > dbScore1 ? "P2 wins!" : "Draw!";
                GUI.color = Color.yellow;
                GUI.Label(new Rect(200f, oy + (dbRows + 1) * cs + 20f, 230f, 30f), winner, gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 380f, 400f, 20f), "<size=11>Click edges between dots. Complete a box = go again!</size>");
        }
        private void DBNewGame()
        {
            dbHoriz = new bool[dbRows + 1, dbCols];
            dbVert = new bool[dbRows, dbCols + 1];
            dbBoxes = new int[dbRows, dbCols];
            dbTurn = 0; dbScore1 = 0; dbScore2 = 0; dbGameOver = false;
        }
        private bool DBCheckBox()
        {
            bool scored = false;
            for (int r = 0; r < dbRows; r++)
                for (int c = 0; c < dbCols; c++)
                    if (dbBoxes[r, c] == 0 && dbHoriz[r, c] && dbHoriz[r + 1, c] && dbVert[r, c] && dbVert[r, c + 1])
                    {
                        dbBoxes[r, c] = dbTurn + 1;
                        if (dbTurn == 0) dbScore1++; else dbScore2++;
                        scored = true;
                    }
            return scored;
        }
        private void DBCheckDone()
        {
            int total = 0;
            for (int r = 0; r < dbRows; r++)
                for (int c = 0; c < dbCols; c++)
                    if (dbBoxes[r, c] > 0) total++;
            if (total == dbRows * dbCols) dbGameOver = true;
        }

        // ==================== CHECKERS 2P ====================
        private void DrawCheckers2P()
        {
            if (ck2Board == null) CK2NewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Checkers 2P</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), ck2GameOver ? (ck2Winner == 1 ? "Red wins!" : "Black wins!") : (ck2Turn == 0 ? "Red's turn" : "Black's turn"));
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { CK2NewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 42f, ox = 210f, oy = 80f;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    bool dark = (r + c) % 2 == 1;
                    GUI.backgroundColor = dark ? new Color(0.3f, 0.2f, 0.1f) : new Color(0.8f, 0.7f, 0.5f);
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (!ck2GameOver)
                        {
                            if (ck2Board[r, c] != 0 && (ck2Board[r, c] == 1 || ck2Board[r, c] == 2) && ((ck2Board[r, c] == 1 && ck2Turn == 0) || (ck2Board[r, c] == 2 && ck2Turn == 1)))
                            { ck2SelR = r; ck2SelC = c; }
                            else if (ck2SelR >= 0 && ck2Board[r, c] == 0)
                            {
                                int dr = r - ck2SelR, dc = c - ck2SelC;
                                int piece = ck2Board[ck2SelR, ck2SelC];
                                bool valid = false;
                                if (piece == 1 && dr == -1 && Mathf.Abs(dc) == 1) valid = true;
                                if (piece == 2 && dr == 1 && Mathf.Abs(dc) == 1) valid = true;
                                if (Mathf.Abs(dr) == 2 && Mathf.Abs(dc) == 2)
                                {
                                    int mr = (ck2SelR + r) / 2, mc = (ck2SelC + c) / 2;
                                    if (ck2Board[mr, mc] != 0 && ck2Board[mr, mc] != piece) valid = true;
                                }
                                if (valid)
                                {
                                    if (Mathf.Abs(dr) == 2)
                                    {
                                        int mr = (ck2SelR + r) / 2, mc = (ck2SelC + c) / 2;
                                        ck2Board[mr, mc] = 0;
                                    }
                                    ck2Board[r, c] = piece;
                                    ck2Board[ck2SelR, ck2SelC] = 0;
                                    if (piece == 1 && r == 0) ck2Board[r, c] = 3;
                                    if (piece == 2 && r == 7) ck2Board[r, c] = 4;
                                    ck2Turn = 1 - ck2Turn;
                                    ck2SelR = ck2SelC = -1;
                                    CK2CheckWin();
                                }
                            }
                            else { ck2SelR = ck2SelC = -1; }
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                    int pieceAtCell = ck2Board[r, c];
                    if (pieceAtCell == 1 || pieceAtCell == 3)
                    {
                        GUI.backgroundColor = pieceAtCell == 3 ? new Color(1f, 0.8f, 0f) : Color.red;
                        GUI.Label(new Rect(x + 10, y + 8, 20, 20), pieceAtCell == 3 ? "K" : "O");
                    }
                    else if (pieceAtCell == 2 || pieceAtCell == 4)
                    {
                        GUI.backgroundColor = pieceAtCell == 4 ? new Color(0.8f, 0.8f, 1f) : Color.black;
                        GUI.Label(new Rect(x + 10, y + 8, 20, 20), pieceAtCell == 4 ? "K" : "O");
                    }
                    if (ck2SelR == r && ck2SelC == c)
                    {
                        GUI.color = Color.yellow;
                        GUI.DrawTexture(new Rect(x, y, cs - 1, cs - 1), Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(170f, 430f, 400f, 20f), "<size=11>Click a piece, then click where to move it</size>");
        }
        private void CK2NewGame()
        {
            ck2Board = new int[8, 8];
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if ((r + c) % 2 == 1 && r < 3) ck2Board[r, c] = 2;
                    else if ((r + c) % 2 == 1 && r > 4) ck2Board[r, c] = 1;
                    else ck2Board[r, c] = 0;
                }
            ck2Turn = 0; ck2SelR = ck2SelC = -1; ck2GameOver = false; ck2Winner = 0;
        }
        private void CK2CheckWin()
        {
            bool p1 = false, p2 = false;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    if (ck2Board[r, c] == 1 || ck2Board[r, c] == 3) p1 = true;
                    if (ck2Board[r, c] == 2 || ck2Board[r, c] == 4) p2 = true;
                }
            if (!p1) { ck2GameOver = true; ck2Winner = 2; }
            else if (!p2) { ck2GameOver = true; ck2Winner = 1; }
        }

        // ==================== SLIDING PUZZLE ====================
        private void DrawSlidingPuzzle()
        {
            if (spGrid == null) SPNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Sliding Puzzle</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Moves: {spMoves}  Best: {(spBest < 0 ? "-" : spBest.ToString())}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { SPNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 60f, ox = 220f, oy = 85f;
            GUIStyle tileStyle = new GUIStyle(GUI.skin.button) { fontSize = 20, fontStyle = FontStyle.Bold };
            for (int r = 0; r < spSize; r++)
                for (int c = 0; c < spSize; c++)
                {
                    int val = spGrid[r, c];
                    GUI.backgroundColor = val == 0 ? new Color(0.2f, 0.2f, 0.25f) : new Color(0.3f, 0.5f, 0.9f);
                    if (val > 0 && GUI.Button(new Rect(ox + c * cs, oy + r * cs, cs - 2, cs - 2), val.ToString(), tileStyle))
                    {
                        if (!spSolved)
                        {
                            for (int dr2 = -1; dr2 <= 1; dr2++)
                                for (int dc2 = -1; dc2 <= 1; dc2++)
                                {
                                    if (Mathf.Abs(dr2) + Mathf.Abs(dc2) != 1) continue;
                                    int nr = r + dr2, nc = c + dc2;
                                    if (nr >= 0 && nr < spSize && nc >= 0 && nc < spSize && spGrid[nr, nc] == 0)
                                    {
                                        spGrid[nr, nc] = val;
                                        spGrid[r, c] = 0;
                                        spMoves++;
                                        SPCheckSolved();
                                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                                        return;
                                    }
                                }
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;
            if (spSolved)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.green;
                GUI.Label(new Rect(200f, oy + spSize * cs + 15f, 230f, 30f), $"Solved in {spMoves} moves!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 380f, 400f, 20f), "<size=11>Click a tile adjacent to the empty space to slide it</size>");
        }
        private void SPNewGame()
        {
            spGrid = new int[spSize, spSize];
            int n = 1;
            for (int r = 0; r < spSize; r++)
                for (int c = 0; c < spSize; c++)
                    spGrid[r, c] = n++;
            spGrid[spSize - 1, spSize - 1] = 0;
            spMoves = 0; spSolved = false;
            for (int i = 0; i < 200; i++)
            {
                List<int[]> empty = new List<int[]>();
                for (int r = 0; r < spSize; r++)
                    for (int c = 0; c < spSize; c++)
                        if (spGrid[r, c] == 0) empty.Add(new[] { r, c });
                int er = empty[0][0], ec = empty[0][1];
                List<int[]> moves2 = new List<int[]>();
                if (er > 0) moves2.Add(new[] { er - 1, ec });
                if (er < spSize - 1) moves2.Add(new[] { er + 1, ec });
                if (ec > 0) moves2.Add(new[] { er, ec - 1 });
                if (ec < spSize - 1) moves2.Add(new[] { er, ec + 1 });
                var m = moves2[UnityEngine.Random.Range(0, moves2.Count)];
                spGrid[er, ec] = spGrid[m[0], m[1]];
                spGrid[m[0], m[1]] = 0;
            }
        }
        private void SPCheckSolved()
        {
            int n = 1;
            for (int r = 0; r < spSize; r++)
                for (int c = 0; c < spSize; c++)
                {
                    if (r == spSize - 1 && c == spSize - 1) { if (spGrid[r, c] != 0) return; }
                    else { if (spGrid[r, c] != n) return; n++; }
                }
            spSolved = true;
            if (spBest < 0 || spMoves < spBest) spBest = spMoves;
        }

        // ==================== BULLS AND COWS ====================
        private void DrawBullsAndCows()
        {
            if (bucSecret[0] == 0 && bucSecret[1] == 0 && bucSecret[2] == 0 && bucSecret[3] == 0) BUCNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Bulls and Cows</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Attempt {bucAttempt}/{bucMaxAttempts}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { BUCNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            GUIStyle hs = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            float y = 75f;
            foreach (string h in bucHistory)
            {
                GUI.Label(new Rect(200f, y, 300f, 18f), h, hs);
                y += 18f;
            }

            if (!bucWon && bucAttempt < bucMaxAttempts)
            {
                GUI.Label(new Rect(200f, y + 10f, 200f, 20f), "<size=12>Enter 4 digits (0-9):</size>");
                bucInput = GUI.TextField(new Rect(200f, y + 35f, 120f, 25f), bucInput);
                GUI.backgroundColor = guiColorA;
                if (GUI.Button(new Rect(330f, y + 35f, 60f, 25f), "Guess"))
                {
                    if (bucInput.Length == 4 && bucInput.All(char.IsDigit))
                    {
                        int[] guess = bucInput.Select(c => c - '0').ToArray();
                        int bulls = 0, cows = 0;
                        for (int i = 0; i < 4; i++)
                            if (guess[i] == bucSecret[i]) bulls++;
                            else if (bucSecret.Contains(guess[i])) cows++;
                        bucAttempt++;
                        bucHistory.Add($"{bucInput}  ->  {bulls}B {cows}C");
                        if (bulls == 4) bucWon = true;
                        bucInput = "";
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
                GUI.backgroundColor = guiColorB;
            }
            if (bucWon)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
                GUI.color = Color.green;
                GUI.Label(new Rect(200f, 350f, 300f, 25f), $"You got it in {bucAttempt} attempts!", gs);
                GUI.color = Color.white;
            }
            else if (bucAttempt >= bucMaxAttempts)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
                GUI.color = Color.red;
                GUI.Label(new Rect(200f, 350f, 300f, 25f), $"Game Over! Code was: {string.Join("", bucSecret)}", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 390f, 400f, 20f), "<size=11>Bulls = right digit, right spot | Cows = right digit, wrong spot</size>");
        }
        private void BUCNewGame()
        {
            bucSecret = new int[] { UnityEngine.Random.Range(1, 10), UnityEngine.Random.Range(0, 10), UnityEngine.Random.Range(0, 10), UnityEngine.Random.Range(0, 10) };
            bucAttempt = 0; bucWon = false; bucInput = "";
            bucHistory = new List<string>();
        }

        // ==================== FREECELL ====================
        private void DrawFreeCell()
        {
            if (fcColumns == null) FCNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>FreeCell</size>");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { FCNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cw = 40f, ch = 55f;
            for (int i = 0; i < 4; i++)
            {
                GUI.backgroundColor = new Color(0.2f, 0.5f, 0.2f);
                GUI.Box(new Rect(185f + i * (cw + 5f), 80f, cw, ch), fcFoundation[i].Count > 0 ? BJCardName(fcFoundation[i][fcFoundation[i].Count - 1]) : "A");
            }

            for (int c = 0; c < 8; c++)
            {
                float x = 185f + c * (cw + 5f);
                GUI.backgroundColor = fcSelectedCol == c ? new Color(0.5f, 0.5f, 0.2f) : new Color(0.3f, 0.3f, 0.35f);
                if (GUI.Button(new Rect(x, 145f, cw, ch - 10), ""))
                {
                    if (fcSelectedCol < 0 && fcColumns[c].Count > 0)
                    {
                        fcSelectedCol = c;
                        fcSelectedIdx = fcColumns[c].Count - 1;
                    }
                    else if (fcSelectedCol == c)
                    {
                        fcSelectedCol = -1; fcSelectedIdx = -1;
                    }
                    else if (fcSelectedCol >= 0)
                    {
                        FCMoveToColumn(c);
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
                for (int r = 0; r < fcColumns[c].Count; r++)
                {
                    int card = fcColumns[c][r];
                    bool faceUp = fcColFaceUp[c][r];
                    GUI.backgroundColor = faceUp ? new Color(0.95f, 0.95f, 0.9f) : new Color(0.2f, 0.2f, 0.6f);
                    GUIStyle cardS = new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter };
                    GUI.Label(new Rect(x + 2, 145f + r * 14, cw - 4, 14), faceUp ? BJCardName(card) : "?", cardS);
                }
            }
            GUI.backgroundColor = guiColorB;
            if (FCCheckWin())
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.green;
                GUI.Label(new Rect(200f, 350f, 250f, 30f), "You Win!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 390f, 400f, 20f), "<size=11>Click a column to select, then click another to move</size>");
        }
        private void FCNewGame()
        {
            List<int> deck = new List<int>();
            for (int i = 1; i <= 52; i++) deck.Add(i);
            for (int i = deck.Count - 1; i > 0; i--) { int j = UnityEngine.Random.Range(0, i + 1); int t = deck[i]; deck[i] = deck[j]; deck[j] = t; }
            fcColumns = new List<int>[8];
            fcColFaceUp = new List<bool>[8];
            fcFoundation = new List<int>[4];
            for (int i = 0; i < 8; i++) { fcColumns[i] = new List<int>(); fcColFaceUp[i] = new List<bool>(); }
            for (int i = 0; i < 4; i++) fcFoundation[i] = new List<int>();
            int idx = 0;
            for (int c = 0; c < 8; c++)
            {
                int count = c < 4 ? 7 : 6;
                for (int r = 0; r < count; r++)
                {
                    fcColumns[c].Add(deck[idx]);
                    fcColFaceUp[c].Add(r == count - 1);
                    idx++;
                }
            }
            fcSelectedCol = -1; fcSelectedIdx = -1;
        }
        private void FCMoveToColumn(int target)
        {
            if (fcSelectedCol < 0) return;
            int card = fcColumns[fcSelectedCol][fcColumns[fcSelectedCol].Count - 1];
            int suit = (card - 1) / 13;
            int rank = (card - 1) % 13;
            if (fcColumns[target].Count > 0)
            {
                int top = fcColumns[target][fcColumns[target].Count - 1];
                int ts = (top - 1) / 13, tr = (top - 1) % 13;
                if ((ts % 2) == (suit % 2) || rank != tr - 1) { fcSelectedCol = -1; return; }
            }
            fcColumns[target].Add(card);
            fcColFaceUp[target].Add(true);
            fcColumns[fcSelectedCol].RemoveAt(fcColumns[fcSelectedCol].Count - 1);
            if (fcColumns[fcSelectedCol].Count > 0)
                fcColFaceUp[fcSelectedCol][fcColFaceUp[fcSelectedCol].Count - 1] = true;
            fcSelectedCol = -1; fcSelectedIdx = -1;
        }
        private bool FCCheckWin()
        {
            for (int i = 0; i < 4; i++)
                if (fcFoundation[i].Count < 13) return false;
            return true;
        }

        // ==================== TRON ====================
        private void DrawTron()
        {
            if (trGrid == null) TRNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Tron / Light Cycles</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), trActive ? "Use WASD or Arrow Keys" : (trAlive ? "You crashed!" : "AI crashed! You win!"));
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { TRNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 16f, ox = 190f, oy = 75f;
            if (trActive)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) trPDir = 0;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) trPDir = 2;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) trPDir = 3;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) trPDir = 1;

                int[] dr2 = { -1, 0, 1, 0 }, dc2 = { 0, 1, 0, -1 };
                int npr = trPR + dr2[trPDir], npc = trPC + dc2[trPDir];
                if (npr < 0 || npr >= trSize || npc < 0 || npc >= trSize || trGrid[npr, npc] != 0)
                { trActive = false; trAlive = false; }
                else { trPR = npr; trPC = npc; trGrid[trPR, trPC] = 1; }

                int ner = trER + dr2[trEDir], nec = trEC + dc2[trEDir];
                bool canFwd = ner >= 0 && ner < trSize && nec >= 0 && nec < trSize && trGrid[ner, nec] == 0;
                if (!canFwd)
                {
                    int[] dirs = { 0, 1, 2, 3 };
                    List<int> valid = new List<int>();
                    foreach (int d in dirs)
                    {
                        int nr2 = trER + dr2[d], nc2 = trEC + dc2[d];
                        if (nr2 >= 0 && nr2 < trSize && nc2 >= 0 && nc2 < trSize && trGrid[nr2, nc2] == 0)
                            valid.Add(d);
                    }
                    if (valid.Count > 0) trEDir = valid[UnityEngine.Random.Range(0, valid.Count)];
                    else { trActive = false; trAlive = true; }
                }
                else { trER = ner; trEC = nec; trGrid[trER, trEC] = 2; }
            }

            for (int r = 0; r < trSize; r++)
                for (int c = 0; c < trSize; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    if (trGrid[r, c] == 1) GUI.backgroundColor = new Color(0.2f, 0.7f, 1f);
                    else if (trGrid[r, c] == 2) GUI.backgroundColor = new Color(1f, 0.4f, 0.1f);
                    else GUI.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
                    GUI.Box(new Rect(x, y, cs - 1, cs - 1), "");
                }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(170f, 400f, 400f, 20f), "<size=11>WASD or Arrow Keys to steer. Don't crash!</size>");
        }
        private void TRNewGame()
        {
            trGrid = new int[trSize, trSize];
            trPR = trSize / 2; trPC = trSize / 4;
            trER = trSize / 2; trEC = trSize * 3 / 4;
            trPDir = 1; trEDir = 3;
            trGrid[trPR, trPC] = 1; trGrid[trER, trEC] = 2;
            trActive = true; trAlive = true; trScore = 0;
        }

        // ==================== BOMBERMAN ====================
        private void DrawBomberman()
        {
            if (bmGrid == null) BMNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Bomberman</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Score: {bmScore}  Lives: {bmLives}  Bombs: {bmBombs}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { BMNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 32f, ox = 200f, oy = 80f;
            if (bmActive)
            {
                if (bmBombPlaced)
                {
                    bmBombTimer -= Time.deltaTime;
                    if (bmBombTimer <= 0)
                    {
                        bmBombPlaced = false;
                        bmGrid[bmBombR, bmBombC] = 0;
                        int[] dr2 = { -1, 0, 1, 0 }, dc2 = { 0, 1, 0, -1 };
                        for (int d = 0; d < 4; d++)
                            for (int i = 1; i <= bmRange; i++)
                            {
                                int nr = bmBombR + dr2[d] * i, nc = bmBombC + dc2[d] * i;
                                if (nr < 0 || nr >= bmSize || nc < 0 || nc >= bmSize || bmGrid[nr, nc] == 1) break;
                                if (bmGrid[nr, nc] == 2) { bmGrid[nr, nc] = 0; bmScore += 10; }
                                bmGrid[bmBombR, bmBombC] = 0;
                            }
                        for (int d = 0; d < 4; d++)
                            for (int i = 1; i <= bmRange; i++)
                            {
                                int nr = bmBombR + dr2[d] * i, nc = bmBombC + dc2[d] * i;
                                if (nr < 0 || nr >= bmSize || nc < 0 || nc >= bmSize || bmGrid[nr, nc] == 1) break;
                                bmGrid[nr, nc] = 3;
                            }
                        bmGrid[bmBombR, bmBombC] = 3;
                    }
                }
                if (!bmBombPlaced)
                    for (int r = 0; r < bmSize; r++)
                        for (int c = 0; c < bmSize; c++)
                            if (bmGrid[r, c] == 3) bmGrid[r, c] = 0;

                bmEnemyTimer -= Time.deltaTime;
                if (bmEnemyTimer <= 0 && bmEnemies.Count > 0)
                {
                    bmEnemyTimer = 0.5f;
                    for (int e = bmEnemies.Count - 1; e >= 0; e--)
                    {
                        int[] dirs = { 0, 1, 2, 3 };
                        int[] dr2 = { -1, 0, 1, 0 }, dc2 = { 0, 1, 0, -1 };
                        List<int> valid = new List<int>();
                        foreach (int d in dirs)
                        {
                            int nr = bmEnemies[e][0] + dr2[d], nc = bmEnemies[e][1] + dc2[d];
                            if (nr >= 0 && nr < bmSize && nc >= 0 && nc < bmSize && (bmGrid[nr, nc] == 0 || bmGrid[nr, nc] == 3))
                                valid.Add(d);
                        }
                        if (valid.Count > 0)
                        {
                            int d2 = valid[UnityEngine.Random.Range(0, valid.Count)];
                            bmGrid[bmEnemies[e][0], bmEnemies[e][1]] = 0;
                            bmEnemies[e][0] += dr2[d2];
                            bmEnemies[e][1] += dc2[d2];
                            bmGrid[bmEnemies[e][0], bmEnemies[e][1]] = 4;
                        }
                    }
                }

                for (int e = bmEnemies.Count - 1; e >= 0; e--)
                    if (bmEnemies[e][0] == bmPR && bmEnemies[e][1] == bmPC)
                    {
                        bmLives--;
                        if (bmLives <= 0) bmActive = false;
                        bmPR = 1; bmPC = 1;
                    }
                if (bmGrid[bmPR, bmPC] == 3) { bmLives--; if (bmLives <= 0) bmActive = false; bmPR = 1; bmPC = 1; }
            }

            for (int r = 0; r < bmSize; r++)
                for (int c = 0; c < bmSize; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    int cell = bmGrid[r, c];
                    if (cell == 1) GUI.backgroundColor = new Color(0.4f, 0.3f, 0.2f);
                    else if (cell == 2) GUI.backgroundColor = new Color(0.2f, 0.6f, 0.2f);
                    else if (cell == 3) GUI.backgroundColor = new Color(1f, 0.6f, 0f);
                    else if (cell == 4) GUI.backgroundColor = new Color(0.8f, 0.2f, 0.2f);
                    else GUI.backgroundColor = new Color(0.15f, 0.15f, 0.18f);

                    if (r == bmPR && c == bmPC) GUI.backgroundColor = Color.cyan;
                    if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (bmActive && bmGrid[r, c] == 0 && Mathf.Abs(r - bmPR) + Mathf.Abs(c - bmPC) == 1)
                        {
                            bmPR = r; bmPC = c;
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;

            if (!bmActive)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.red;
                GUI.Label(new Rect(200f, oy + bmSize * cs + 10f, 200f, 30f), "Game Over!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 400f, 400f, 20f), "<size=11>Click adjacent tiles to move. Destroy blocks (green), avoid enemies (red)!</size>");
        }
        private void BMNewGame()
        {
            bmGrid = new int[bmSize, bmSize];
            for (int r = 0; r < bmSize; r++)
                for (int c = 0; c < bmSize; c++)
                {
                    if (r % 2 == 0 && c % 2 == 0) bmGrid[r, c] = 1;
                    else if (UnityEngine.Random.Range(0, 3) == 0) bmGrid[r, c] = 2;
                    else bmGrid[r, c] = 0;
                }
            bmGrid[1, 1] = 0; bmGrid[1, 2] = 0; bmGrid[2, 1] = 0;
            bmPR = 1; bmPC = 1; bmBombs = 1; bmRange = 2; bmLives = 3; bmScore = 0; bmActive = true;
            bmBombPlaced = false; bmBombTimer = 0; bmEnemyTimer = 0.5f;
            bmEnemies = new List<int[]>();
            for (int i = 0; i < 3; i++)
            {
                int er, ec;
                do { er = UnityEngine.Random.Range(1, bmSize); ec = UnityEngine.Random.Range(1, bmSize); }
                while (bmGrid[er, ec] != 0);
                bmEnemies.Add(new[] { er, ec });
                bmGrid[er, ec] = 4;
            }
        }

        // ==================== BRICK CALCULATOR ====================
        private void DrawBrickCalculator()
        {
            if (brcGrid == null) BRCNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Brick Calculator</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), brcWon ? "You win!" : brcGameOver ? "Boom!" : $"Bombs: {brcBombs}  Flags: {brcFlagsLeft}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { BRCNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;
            GUI.backgroundColor = brcFlagMode ? new Color(0.8f, 0.4f, 0.2f) : guiColorB;
            if (GUI.Button(new Rect(420f, 46f, 75f, 22f), brcFlagMode ? "Flag On" : "Flag Off"))
            { brcFlagMode = !brcFlagMode; SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 38f, ox = 200f, oy = 80f;
            Color[] nums = { Color.white, Color.blue, Color.green, Color.red, new Color(0, 0, 0.5f), new Color(0.5f, 0, 0), Color.cyan, Color.black, Color.gray };
            for (int r = 0; r < brcSize; r++)
                for (int c = 0; c < brcSize; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    if (brcRevealed[r, c])
                    {
                        if (brcGrid[r, c] == -1)
                        {
                            GUI.backgroundColor = new Color(1f, 0.2f, 0.2f);
                            GUI.Label(new Rect(x + 10, y + 8, 20, 20), "*");
                        }
                        else
                        {
                            GUI.backgroundColor = new Color(0.85f, 0.85f, 0.8f);
                            GUIStyle ns = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                            GUI.color = brcGrid[r, c] > 0 && brcGrid[r, c] < nums.Length ? nums[brcGrid[r, c]] : Color.black;
                            GUI.Label(new Rect(x, y, cs, cs), brcGrid[r, c] > 0 ? brcGrid[r, c].ToString() : "", ns);
                            GUI.color = Color.white;
                        }
                    }
                    else
                    {
                        GUI.backgroundColor = brcFlagged[r, c] ? new Color(0.8f, 0.4f, 0.2f) : new Color(0.4f, 0.4f, 0.45f);
                        if (GUI.Button(new Rect(x, y, cs - 1, cs - 1), brcFlagged[r, c] ? "F" : ""))
                        {
                            if (!brcGameOver && !brcWon)
                            {
                                if (brcFlagMode)
                                {
                                    brcFlagged[r, c] = !brcFlagged[r, c];
                                    brcFlagsLeft += brcFlagged[r, c] ? -1 : 1;
                                }
                                else
                                {
                                    brcRevealed[r, c] = true;
                                    if (brcGrid[r, c] == -1) { brcGameOver = true; BRCRevealAll(); }
                                    else if (brcGrid[r, c] == 0) BRCFloodFill(r, c);
                                    BRCCheckWin();
                                }
                                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                            }
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(170f, 400f, 400f, 20f), "<size=11>Click to reveal, toggle Flag mode to mark bombs</size>");
        }
        private void BRCNewGame()
        {
            brcGrid = new int[brcSize, brcSize];
            brcRevealed = new bool[brcSize, brcSize];
            brcFlagged = new bool[brcSize, brcSize];
            brcGameOver = false; brcWon = false; brcFlagMode = false;
            brcBombs = 10; brcFlagsLeft = brcBombs;
            for (int i = 0; i < brcBombs; i++)
            {
                int r, c;
                do { r = UnityEngine.Random.Range(0, brcSize); c = UnityEngine.Random.Range(0, brcSize); }
                while (brcGrid[r, c] == -1);
                brcGrid[r, c] = -1;
            }
            for (int r = 0; r < brcSize; r++)
                for (int c = 0; c < brcSize; c++)
                {
                    if (brcGrid[r, c] == -1) continue;
                    int cnt = 0;
                    for (int dr2 = -1; dr2 <= 1; dr2++)
                        for (int dc2 = -1; dc2 <= 1; dc2++)
                        {
                            int nr = r + dr2, nc = c + dc2;
                            if (nr >= 0 && nr < brcSize && nc >= 0 && nc < brcSize && brcGrid[nr, nc] == -1) cnt++;
                        }
                    brcGrid[r, c] = cnt;
                }
        }
        private void BRCFloodFill(int r, int c)
        {
            if (r < 0 || r >= brcSize || c < 0 || c >= brcSize || brcRevealed[r, c] || brcGrid[r, c] == -1) return;
            brcRevealed[r, c] = true;
            if (brcGrid[r, c] == 0)
            {
                for (int dr2 = -1; dr2 <= 1; dr2++)
                    for (int dc2 = -1; dc2 <= 1; dc2++)
                        BRCFloodFill(r + dr2, c + dc2);
            }
        }
        private void BRCRevealAll()
        {
            for (int r = 0; r < brcSize; r++)
                for (int c = 0; c < brcSize; c++)
                    brcRevealed[r, c] = true;
        }
        private void BRCCheckWin()
        {
            for (int r = 0; r < brcSize; r++)
                for (int c = 0; c < brcSize; c++)
                    if (!brcRevealed[r, c] && brcGrid[r, c] != -1) return;
            brcWon = true;
        }

        // ==================== OTHELLO ====================
        private void DrawOthello()
        {
            if (othBoard == null) OTHNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Othello (Reversi)</size>");
            int p1 = 0, p2 = 0;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (othBoard[r, c] == 1) p1++;
                    else if (othBoard[r, c] == 2) p2++;
            GUI.Label(new Rect(170f, 48f, 400f, 20f), othGameOver ? $"Game Over! Black: {p1} White: {p2}" : (othTurn == 0 ? "Black's turn" : "White's turn") + $"  Black: {p1} White: {p2}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { OTHNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 42f, ox = 210f, oy = 80f;
            bool hasMove = false;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (othBoard[r, c] == 0 && OTHCanPlace(r, c, othTurn + 1)) hasMove = true;

            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    GUI.backgroundColor = new Color(0.1f, 0.5f, 0.1f);
                    GUI.Box(new Rect(x, y, cs - 1, cs - 1), "");
                    if (othBoard[r, c] != 0)
                    {
                        GUI.backgroundColor = othBoard[r, c] == 1 ? Color.black : Color.white;
                        float ps = cs * 0.7f;
                        GUI.Box(new Rect(x + (cs - ps) / 2, y + (cs - ps) / 2, ps, ps), "");
                    }
                    if (!othGameOver && hasMove && othTurn == 0 && GUI.Button(new Rect(x, y, cs - 1, cs - 1), ""))
                    {
                        if (othBoard[r, c] == 0 && OTHCanPlace(r, c, 1))
                        {
                            OTHPlace(r, c, 1);
                            othTurn = 1;
                            if (!OTHHasMove(2)) othTurn = 0;
                            OTHCheckEnd();
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                }
            GUI.backgroundColor = guiColorB;

            if (!othGameOver && othTurn == 1)
            {
                Invoke(nameof(OTHAIMove), 0.3f);
            }
            GUI.Label(new Rect(170f, 430f, 400f, 20f), "<size=11>Click to place a stone and flip opponent pieces</size>");
        }
        private void OTHNewGame()
        {
            othBoard = new int[8, 8];
            othBoard[3, 3] = 2; othBoard[3, 4] = 1;
            othBoard[4, 3] = 1; othBoard[4, 4] = 2;
            othTurn = 0; othGameOver = false;
        }
        private bool OTHCanPlace(int r, int c, int p)
        {
            if (othBoard[r, c] != 0) return false;
            int[] dr2 = { -1, -1, -1, 0, 0, 1, 1, 1 }, dc2 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            for (int d = 0; d < 8; d++)
            {
                int nr = r + dr2[d], nc = c + dc2[d], cnt = 0;
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8 && othBoard[nr, nc] != 0 && othBoard[nr, nc] != p)
                { cnt++; nr += dr2[d]; nc += dc2[d]; }
                if (cnt > 0 && nr >= 0 && nr < 8 && nc >= 0 && nc < 8 && othBoard[nr, nc] == p) return true;
            }
            return false;
        }
        private void OTHPlace(int r, int c, int p)
        {
            othBoard[r, c] = p;
            int[] dr2 = { -1, -1, -1, 0, 0, 1, 1, 1 }, dc2 = { -1, 0, 1, -1, 1, -1, 0, 1 };
            for (int d = 0; d < 8; d++)
            {
                int nr = r + dr2[d], nc = c + dc2[d], cnt = 0;
                List<int[]> toFlip = new List<int[]>();
                while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8 && othBoard[nr, nc] != 0 && othBoard[nr, nc] != p)
                { toFlip.Add(new[] { nr, nc }); nr += dr2[d]; nc += dc2[d]; cnt++; }
                if (cnt > 0 && nr >= 0 && nr < 8 && nc >= 0 && nc < 8 && othBoard[nr, nc] == p)
                    foreach (var f in toFlip) othBoard[f[0], f[1]] = p;
            }
        }
        private bool OTHHasMove(int p)
        {
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (OTHCanPlace(r, c, p)) return true;
            return false;
        }
        private void OTHCheckEnd()
        {
            if (!OTHHasMove(1) && !OTHHasMove(2)) othGameOver = true;
        }
        private void OTHAIMove()
        {
            if (othGameOver) return;
            int bestR = -1, bestC = -1, bestScore = -1;
            for (int r = 0; r < 8; r++)
                for (int c = 0; c < 8; c++)
                    if (othBoard[r, c] == 0 && OTHCanPlace(r, c, 2))
                    {
                        int sc = 0;
                        int[] dr2 = { -1, -1, -1, 0, 0, 1, 1, 1 }, dc2 = { -1, 0, 1, -1, 1, -1, 0, 1 };
                        for (int d = 0; d < 8; d++)
                        {
                            int nr = r + dr2[d], nc = c + dc2[d];
                            while (nr >= 0 && nr < 8 && nc >= 0 && nc < 8 && othBoard[nr, nc] != 0 && othBoard[nr, nc] != 2)
                            { sc++; nr += dr2[d]; nc += dc2[d]; }
                        }
                        if (r == 0 && (c == 0 || c == 7)) sc += 50;
                        else if (r == 7 && (c == 0 || c == 7)) sc += 50;
                        else if (r == 0 || r == 7 || c == 0 || c == 7) sc += 10;
                        if (sc > bestScore) { bestScore = sc; bestR = r; bestC = c; }
                    }
            if (bestR >= 0)
            {
                OTHPlace(bestR, bestC, 2);
                othTurn = 0;
                if (!OTHHasMove(1)) othTurn = 1;
                OTHCheckEnd();
            }
            else { othTurn = 0; OTHCheckEnd(); }
        }

        // ==================== RUSH HOUR ====================
        private void DrawRushHour()
        {
            if (rhGrid == null) RHNewGame();
            GUI.Label(new Rect(170f, 25f, 300f, 30f), "<size=18>Rush Hour</size>");
            GUI.Label(new Rect(170f, 48f, 300f, 20f), $"Moves: {rhMoves}");
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(500f, 46f, 80f, 22f), "New Game"))
            { RHNewGame(); SoundManager.Play(SoundManager.DefaultSounds["Button"]); }
            GUI.backgroundColor = guiColorB;

            float cs = 50f, ox = 210f, oy = 85f;
            Color[] carColors = { Color.red, Color.blue, new Color(0.2f, 0.7f, 0.2f), new Color(0.8f, 0.8f, 0.2f),
                new Color(0.8f, 0.3f, 0.8f), new Color(0.2f, 0.8f, 0.8f), new Color(0.9f, 0.5f, 0.2f) };
            for (int r = 0; r < rhSize; r++)
                for (int c = 0; c < rhSize; c++)
                {
                    float x = ox + c * cs, y = oy + r * cs;
                    if (rhGrid[r, c] == -1)
                        GUI.backgroundColor = new Color(0.1f, 0.7f, 0.1f, 0.5f);
                    else if (rhGrid[r, c] > 0)
                        GUI.backgroundColor = carColors[(rhGrid[r, c] - 1) % carColors.Length];
                    else
                        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.3f);

                    if (GUI.Button(new Rect(x, y, cs - 2, cs - 2), rhGrid[r, c] == 1 ? ">>" : ""))
                    {
                        if (rhSelected < 0) { rhSelected = rhGrid[r, c]; }
                        else if (rhSelected == rhGrid[r, c]) { rhSelected = -1; }
                        else
                        {
                            int dir = rhCarDir[rhSelected - 1];
                            if (dir == 0)
                            {
                                if (r == rhCarR[rhSelected - 1] && Mathf.Abs(c - rhCarC[rhSelected - 1]) == 1)
                                {
                                    if (c < rhCarC[rhSelected - 1]) RHMoveCar(rhSelected, -1);
                                    else RHMoveCar(rhSelected, 1);
                                }
                            }
                            else
                            {
                                if (c == rhCarC[rhSelected - 1] && Mathf.Abs(r - rhCarR[rhSelected - 1]) == 1)
                                {
                                    if (r < rhCarR[rhSelected - 1]) RHMoveCar(rhSelected, -1);
                                    else RHMoveCar(rhSelected, 1);
                                }
                            }
                            rhSelected = -1;
                        }
                        SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                    }
                }
            GUI.backgroundColor = guiColorB;

            if (rhSelected > 0)
            {
                GUI.Label(new Rect(200f, oy + rhSize * cs + 10f, 300f, 20f), $"<size=11>Car {rhSelected} selected. Click an adjacent empty cell to move.</size>");
            }

            if (rhSolved)
            {
                GUIStyle gs = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                GUI.color = Color.green;
                GUI.Label(new Rect(200f, oy + rhSize * cs + 35f, 200f, 30f), $"Solved in {rhMoves} moves!", gs);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(170f, 420f, 400f, 20f), "<size=11>Click a car, then click adjacent empty cell to slide it. Get the red car to the exit!</size>");
        }
        private void RHNewGame()
        {
            rhGrid = new int[rhSize, rhSize];
            rhCarR = new int[6]; rhCarC = new int[6]; rhCarLen = new int[6]; rhCarDir = new int[6];
            rhMoves = 0; rhSolved = false; rhSelected = -1;
            for (int i = 0; i < 6; i++)
            {
                rhCarDir[i] = i < 2 ? 0 : 1;
                rhCarLen[i] = i == 0 ? 3 : UnityEngine.Random.Range(2, 4);
            }
            rhCarR[0] = 2; rhCarC[0] = 0; rhCarLen[0] = 3; rhCarDir[0] = 0;
            for (int i = 1; i < 6; i++)
            {
                bool placed = false;
                int attempts = 0;
                while (!placed && attempts < 100)
                {
                    attempts++;
                    rhCarR[i] = UnityEngine.Random.Range(0, rhSize);
                    rhCarC[i] = UnityEngine.Random.Range(0, rhSize);
                    rhCarLen[i] = UnityEngine.Random.Range(2, 4);
                    bool ok = true;
                    for (int l = 0; l < rhCarLen[i]; l++)
                    {
                        int r = rhCarDir[i] == 0 ? rhCarR[i] : rhCarR[i] + l;
                        int c = rhCarDir[i] == 0 ? rhCarC[i] + l : rhCarC[i];
                        if (r < 0 || r >= rhSize || c < 0 || c >= rhSize || rhGrid[r, c] != 0) { ok = false; break; }
                    }
                    if (ok)
                    {
                        for (int l = 0; l < rhCarLen[i]; l++)
                        {
                            int r = rhCarDir[i] == 0 ? rhCarR[i] : rhCarR[i] + l;
                            int c = rhCarDir[i] == 0 ? rhCarC[i] + l : rhCarC[i];
                            rhGrid[r, c] = i + 1;
                        }
                        placed = true;
                    }
                }
            }
            rhGrid[2, 5] = -1;
        }
        private void RHMoveCar(int car, int delta)
        {
            int dir = rhCarDir[car - 1];
            int len = rhCarLen[car - 1];
            int newR = rhCarR[car - 1] + (dir == 1 ? delta : 0);
            int newC = rhCarC[car - 1] + (dir == 0 ? delta : 0);
            int endR = dir == 0 ? newR : newR + len - 1;
            int endC = dir == 0 ? newC + len - 1 : newC;
            if (newR < 0 || newR >= rhSize || newC < 0 || newC >= rhSize || endR < 0 || endR >= rhSize || endC < 0 || endC >= rhSize) return;
            for (int l = 0; l < len; l++)
            {
                int r = dir == 0 ? newR : newR + l;
                int c = dir == 0 ? newC + l : newC;
                if (rhGrid[r, c] != 0 && rhGrid[r, c] != car) return;
            }
            for (int l = 0; l < len; l++)
            {
                int r = dir == 0 ? rhCarR[car - 1] : rhCarR[car - 1] + l;
                int c = dir == 0 ? rhCarC[car - 1] + l : rhCarC[car - 1];
                rhGrid[r, c] = 0;
            }
            rhCarR[car - 1] = newR;
            rhCarC[car - 1] = newC;
            for (int l = 0; l < len; l++)
            {
                int r = dir == 0 ? newR : newR + l;
                int c = dir == 0 ? newC + l : newC;
                rhGrid[r, c] = car;
            }
            rhMoves++;
            if (rhCarR[0] == 2 && rhCarC[0] + rhCarLen[0] - 1 >= rhSize - 1)
                rhSolved = true;
        }

        private Vector2 supportScrollPosition;
        private string supportLookupInput = "";
        private string supportLookupResult = "";
        private Color supportLookupColor = Color.white;
        private string reportModName = "";
        private string reportDescription = "";
        private string reportScreenshotUrl = "";
        private string reportStatus = "";
        private float reportStatusTimer;
        private List<SupportReportEntry> reportEntries = new List<SupportReportEntry>();
        private Vector2 reportListScroll;

        private struct SupportReportEntry
        {
            public string sender;
            public string modName;
            public string description;
            public string screenshotUrl;
            public string timestamp;
        }

        private void DrawSupportTab()
        {
            float contentW = 510f;
            float totalH = 1200f;

            reportListScroll = GUI.BeginScrollView(
                new Rect(170f, 21f, 530f, 395f),
                reportListScroll,
                new Rect(0f, 0f, contentW, totalH),
                false, true);

            float y = 4f;
            float x = 6f;

            GUI.backgroundColor = guiColorA;
            GUI.Label(new Rect(x, y, contentW, 22f), "<b>Report a Broken Mod</b>");
            y += 24f;

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(x, y, contentW, 18f), "Mod Name:");
            y += 18f;
            reportModName = GUI.TextField(new Rect(x, y, contentW, 22f), reportModName);
            y += 28f;

            GUI.Label(new Rect(x, y, contentW, 18f), "Description of the issue:");
            y += 18f;
            GUI.backgroundColor = new Color(0.12f, 0.12f, 0.17f, 0.95f);
            reportDescription = GUI.TextArea(new Rect(x, y, contentW, 80f), reportDescription);
            y += 86f;

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(x, y, contentW, 18f), "Screenshot URL (optional):");
            y += 18f;
            reportScreenshotUrl = GUI.TextField(new Rect(x, y, contentW, 22f), reportScreenshotUrl);
            y += 28f;

            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(x, y, 150f, 25f), "Submit Report"))
            {
                if (string.IsNullOrWhiteSpace(reportModName) || string.IsNullOrWhiteSpace(reportDescription))
                {
                    reportStatus = "<color=red>Please fill in the mod name and description.</color>";
                    reportStatusTimer = Time.time;
                }
                else
                {
                    SubmitSupportReport(reportModName, reportDescription, reportScreenshotUrl);
                    reportModName = "";
                    reportDescription = "";
                    reportScreenshotUrl = "";
                    reportStatus = "<color=green>Report submitted! The owner will be notified.</color>";
                    reportStatusTimer = Time.time;
                }
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += 30f;

            if (!string.IsNullOrEmpty(reportStatus) && Time.time - reportStatusTimer < 5f)
            {
                Color prevContent = GUI.contentColor;
                GUI.contentColor = Color.white;
                GUI.Label(new Rect(x, y, contentW, 20f), reportStatus);
                GUI.contentColor = prevContent;
                y += 22f;
            }
            y += 10f;

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(x, y, contentW, 20f), "<b>Submitted Reports</b>");
            y += 22f;

            if (reportEntries.Count == 0)
            {
                GUI.Label(new Rect(x + 10f, y, contentW - 10f, 18f), "<color=#666666>No reports submitted yet.</color>");
                y += 20f;
            }
            else
            {
                for (int i = reportEntries.Count - 1; i >= 0; i--)
                {
                    var r = reportEntries[i];
                    GUI.backgroundColor = new Color(0.12f, 0.12f, 0.17f, 0.95f);
                    GUI.Box(new Rect(x, y, contentW, 70f), "");
                    GUI.backgroundColor = guiColorA;
                    GUI.Label(new Rect(x + 6f, y + 2f, contentW - 12f, 18f), "<b>" + r.modName + "</b>  <color=#888888>by " + r.sender + "</color>");
                    GUI.backgroundColor = guiColorB;
                    string desc = r.description.Length > 120 ? r.description.Substring(0, 117) + "..." : r.description;
                    GUI.Label(new Rect(x + 6f, y + 20f, contentW - 12f, 30f), desc);
                    GUI.Label(new Rect(x + 6f, y + 50f, contentW - 12f, 16f), "<color=#666666>" + r.timestamp + "</color>");
                    if (!string.IsNullOrEmpty(r.screenshotUrl))
                    {
                        if (GUI.Button(new Rect(x + contentW - 90f, y + 48f, 84f, 18f), "Screenshot"))
                        {
                            Application.OpenURL(r.screenshotUrl);
                            SoundManager.Play(SoundManager.DefaultSounds["Button"]);
                        }
                    }
                    y += 76f;
                }
            }
            y += 10f;

            GUI.backgroundColor = guiColorA;
            GUI.Label(new Rect(x, y, contentW, 20f), "<b>Player Role Lookup</b>");
            y += 22f;
            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(x, y, contentW, 18f), "Type a player name or User ID to check their role:");
            y += 20f;

            GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
            GUI.SetNextControlName("SupportLookup");
            supportLookupInput = GUI.TextField(new Rect(x, y, 370f, 22f), supportLookupInput);
            GUI.backgroundColor = guiColorA;
            if (GUI.Button(new Rect(x + 380f, y, 120f, 22f), "Lookup"))
            {
                supportLookupResult = "";
                string input = supportLookupInput.Trim();
                if (string.IsNullOrEmpty(input))
                {
                    supportLookupResult = "Enter a name or User ID.";
                    supportLookupColor = Color.yellow;
                }
                else
                {
                    bool isUserId = false;
                    foreach (NetPlayer p in PhotonNetwork.PlayerList)
                    {
                        if (p.UserId == input)
                        {
                            input = p.NickName;
                            isUserId = true;
                            break;
                        }
                    }

                    string matchedName = null;
                    string matchedUserId = null;

                    foreach (var kvp in ServerData.Administrators)
                    {
                        if (kvp.Key == input || kvp.Value.Equals(input, StringComparison.OrdinalIgnoreCase))
                        {
                            matchedUserId = kvp.Key;
                            matchedName = kvp.Value;
                            break;
                        }
                    }

                    if (matchedName == null && isUserId)
                    {
                        foreach (var kvp in ServerData.Administrators)
                        {
                            if (kvp.Key == input)
                            {
                                matchedUserId = kvp.Key;
                                matchedName = kvp.Value;
                                break;
                            }
                        }
                    }

                    if (matchedName != null)
                    {
                        if (ServerData.OwnerUserIds.Contains(matchedUserId))
                        {
                            supportLookupResult = matchedName + " (" + matchedUserId + ") is an OWNER.";
                            supportLookupColor = new Color(1f, 0.84f, 0f);
                        }
                        else if (ServerData.SuperAdministrators.Contains(matchedName))
                        {
                            supportLookupResult = matchedName + " (" + matchedUserId + ") is a SUPER-ADMIN.";
                            supportLookupColor = new Color(0.8f, 0.2f, 1f);
                        }
                        else
                        {
                            supportLookupResult = matchedName + " (" + matchedUserId + ") is an ADMIN.";
                            supportLookupColor = new Color(0.3f, 0.9f, 0.4f);
                        }
                    }
                    else
                    {
                        supportLookupResult = input + " is not an admin/owner.";
                        supportLookupColor = new Color(0.6f, 0.6f, 0.6f);
                    }
                }
                SoundManager.Play(SoundManager.DefaultSounds["Button"]);
            }
            y += 28f;

            if (!string.IsNullOrEmpty(supportLookupResult))
            {
                Color prevContent = GUI.contentColor;
                GUI.contentColor = supportLookupColor;
                GUI.Label(new Rect(x, y, contentW, 20f), supportLookupResult);
                GUI.contentColor = prevContent;
                y += 24f;
            }
            y += 10f;

            GUI.backgroundColor = guiColorB;
            GUI.Label(new Rect(x, y, contentW, 20f), "<b>All Owners</b>");
            y += 20f;
            foreach (string uid in ServerData.OwnerUserIds)
            {
                string name = ServerData.Administrators.ContainsKey(uid) ? ServerData.Administrators[uid] : "(unknown)";
                GUI.Label(new Rect(x + 10f, y, contentW - 10f, 16f), "<color=#FFD700>" + name + "</color>  <color=#888888>" + uid + "</color>");
                y += 16f;
            }
            y += 6f;

            GUI.Label(new Rect(x, y, contentW, 20f), "<b>All Super-Admins</b>");
            y += 20f;
            foreach (string saName in ServerData.SuperAdministrators)
            {
                GUI.Label(new Rect(x + 10f, y, contentW - 10f, 16f), "<color=#CC33FF>" + saName + "</color>");
                y += 16f;
            }
            y += 6f;

            GUI.Label(new Rect(x, y, contentW, 20f), "<b>All Admins</b>");
            y += 20f;
            foreach (var kvp in ServerData.Administrators)
            {
                string uid = kvp.Key;
                string name = kvp.Value;
                bool isOwner = ServerData.OwnerUserIds.Contains(uid);
                bool isSuper = ServerData.SuperAdministrators.Contains(name);
                if (isOwner)
                    GUI.Label(new Rect(x + 10f, y, contentW - 10f, 16f), "<color=#FFD700>" + name + "</color>  <color=#888888>" + uid + "</color>  <color=#FFD700>[Owner]</color>");
                else if (isSuper)
                    GUI.Label(new Rect(x + 10f, y, contentW - 10f, 16f), "<color=#CC33FF>" + name + "</color>  <color=#888888>" + uid + "</color>  <color=#CC33FF>[Super-Admin]</color>");
                else
                    GUI.Label(new Rect(x + 10f, y, contentW - 10f, 16f), "<color=#4DE64D>" + name + "</color>  <color=#888888>" + uid + "</color>");
                y += 16f;
            }

            GUI.EndScrollView();
        }

        private async void SubmitSupportReport(string modName, string description, string screenshotUrl)
        {
            try
            {
                string nick = string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName) ? "Unknown" : PhotonNetwork.LocalPlayer.NickName;
                string uid = PhotonNetwork.LocalPlayer.UserId ?? "unknown";
                string timestamp = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss");

                reportEntries.Add(new SupportReportEntry { sender = nick, modName = modName, description = description, screenshotUrl = screenshotUrl ?? "", timestamp = timestamp });

                string webhook = "https://discord.com/api/webhooks/1523079492975853679/IW5B1EshhbhK42hqkW2jOUjLQTLE96L7DI1QP5zZQPGn2m__X2DL1bb1IRkKpO1pXdMY";

                string content = $"**New Support Report**\n**Mod:** {modName}\n**Issue:** {description}\n**From:** {nick} (`{uid}`)\n**Time:** {timestamp}";
                if (!string.IsNullOrEmpty(screenshotUrl))
                    content += $"\n**Screenshot:** {screenshotUrl}";

                using (var client = new System.Net.Http.HttpClient())
                {
                    var payload = new System.Net.Http.StringContent(
                        "{\"content\":\"" + content.Replace("\n", "\\n").Replace("\"", "\\\"") + "\"}",
                        System.Text.Encoding.UTF8,
                        "application/json"
                    );
                    await client.PostAsync(webhook, payload);
                }
            }
            catch { }
        }
    }
}
