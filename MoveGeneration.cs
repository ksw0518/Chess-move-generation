// See https://aka.ms/new-console-template for more information


using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Intrinsics.X86;
using System.Xml.Serialization;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Turbulence.Engine;
using System.Diagnostics;

namespace Turbulence
{

    class Engine
    {
        const int promotionFlag = 0x1000;
        const int captureFlag = 0x0100;
        const int special1Flag = 0x0010;
        const int special0Flag= 0x0001;

        const int quiet_move = 0;
        const int double_pawn_push = special0Flag;
        const int king_castle = special1Flag;
        const int queen_castle = special0Flag | special1Flag;
        const int capture = captureFlag;    
        const int ep_capture = captureFlag | special0Flag;    
        const int knight_promo = promotionFlag;    
        const int bishop_promo = promotionFlag | special0Flag;    
        const int rook_promo = promotionFlag | special1Flag;    
        const int queen_promo = promotionFlag | special1Flag | special0Flag;
        const int knight_promo_capture = knight_promo | capture;
        const int bishop_promo_capture = bishop_promo | capture;
        const int rook_promo_capture = rook_promo | capture;
        const int queen_promo_capture = queen_promo | capture;




        //ulong[] bitboards = new ulong[12];
        //ulong[] occupancies = new ulong[3];
        //int side;
        //int enpassent = (int)Square.no_sq;
        //ulong castle;

        uint state = 1804289383;
        const ulong NotAFile = 18374403900871474942;
        const ulong NotHFile = 9187201950435737471;
        const ulong NotHGFile = 4557430888798830399;
        const ulong NotABFile = 18229723555195321596;

         ulong[,] pawn_attacks = new ulong[2, 64];
        ulong[] Knight_attacks = new ulong[64];
        ulong[] King_attacks = new ulong[64];

        ulong[] bishop_masks = new ulong[64];
        ulong[] rook_masks = new ulong[64];

        ulong[,] bishop_attacks = new ulong[64, 512];
        ulong[,] rook_attacks = new ulong[64, 4096];

        ulong[,] betweenTable = new ulong[64, 64];

        const ulong WhiteKingCastle = 0x0001;
        const ulong WhiteQueenCastle = 0x0010;
        const ulong BlackKingCastle = 0x0100;
        const ulong BlackQueenCastle = 0x1000;


        const string empty_board = "8/8/8/8/8/8/8/8 w - - ";
        const string start_position = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ";
        const string tricky_position = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1 ";
        const string killer_position = "rnbqkblr/pplp1pPp/8/2p1pP2/1P1P4/3P3P/P1P1P3/RNBQKBNR w KQkq e6 0 1";
        const string cmk_position = "r2q1rk1/ppp2ppp/2n1bn2/2b1p3/3pP3/3P1NPP/PPP1NPB1/R1BQ1RK1 b - - 0 9 ";
        const string pawn_test = "8/pppppppp/8/8/8/8/PPPPPPPP/8 w - - 0 1"; //illegal position
        enum Square
        {
            a8, b8, c8, d8, e8, f8, g8, h8,
            a7, b7, c7, d7, e7, f7, g7, h7,
            a6, b6, c6, d6, e6, f6, g6, h6,

            a5, b5, c5, d5, e5, f5, g5, h5,
            a4, b4, c4, d4, e4, f4, g4, h4,
            a3, b3, c3, d3, e3, f3, g3, h3,
            a2, b2, c2, d2, e2, f2, g2, h2,
            a1, b1, c1, d1, e1, f1, g1, h1, no_sq
        }
        
        int[] bishop_relevant_bits =
        {
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
        };

        int[] rook_relevant_bits =
        {
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12,
        };

        //magic numbers
        ulong[] rook_magic_numbers = new ulong[64] {0x8A80104000800020UL,
0x140002000100040UL,
0x2801880A0017001UL,
0x100081001000420UL,
0x200020010080420UL,
0x3001C0002010008UL,
0x8480008002000100UL,
0x2080088004402900UL,
0x800098204000UL,
0x2024401000200040UL,
0x100802000801000UL,
0x120800800801000UL,
0x208808088000400UL,
0x2802200800400UL,
0x2200800100020080UL,
0x801000060821100UL,
0x80044006422000UL,
0x100808020004000UL,
0x12108A0010204200UL,
0x140848010000802UL,
0x481828014002800UL,
0x8094004002004100UL,
0x4010040010010802UL,
0x20008806104UL,
0x100400080208000UL,
0x2040002120081000UL,
0x21200680100081UL,
0x20100080080080UL,
0x2000A00200410UL,
0x20080800400UL,
0x80088400100102UL,
0x80004600042881UL,
0x4040008040800020UL,
0x440003000200801UL,
0x4200011004500UL,
0x188020010100100UL,
0x14800401802800UL,
0x2080040080800200UL,
0x124080204001001UL,
0x200046502000484UL,
0x480400080088020UL,
0x1000422010034000UL,
0x30200100110040UL,
0x100021010009UL,
0x2002080100110004UL,
0x202008004008002UL,
0x20020004010100UL,
0x2048440040820001UL,
0x101002200408200UL,
0x40802000401080UL,
0x4008142004410100UL,
0x2060820C0120200UL,
0x1001004080100UL,
0x20C020080040080UL,
0x2935610830022400UL,
0x44440041009200UL,
0x280001040802101UL,
0x2100190040002085UL,
0x80C0084100102001UL,
0x4024081001000421UL,
0x20030A0244872UL,
0x12001008414402UL,
0x2006104900A0804UL,
0x1004081002402UL};
        ulong[] bishop_magic_numbers = new ulong[64]
        {
            0x40040844404084UL,
0x2004208A004208UL,
0x10190041080202UL,
0x108060845042010UL,
0x581104180800210UL,
0x2112080446200010UL,
0x1080820820060210UL,
0x3C0808410220200UL,
0x4050404440404UL,
0x21001420088UL,
0x24D0080801082102UL,
0x1020A0A020400UL,
0x40308200402UL,
0x4011002100800UL,
0x401484104104005UL,
0x801010402020200UL,
0x400210C3880100UL,
0x404022024108200UL,
0x810018200204102UL,
0x4002801A02003UL,
0x85040820080400UL,
0x810102C808880400UL,
0xE900410884800UL,
0x8002020480840102UL,
0x220200865090201UL,
0x2010100A02021202UL,
0x152048408022401UL,
0x20080002081110UL,
0x4001001021004000UL,
0x800040400A011002UL,
0xE4004081011002UL,
0x1C004001012080UL,
0x8004200962A00220UL,
0x8422100208500202UL,
0x2000402200300C08UL,
0x8646020080080080UL,
0x80020A0200100808UL,
0x2010004880111000UL,
0x623000A080011400UL,
0x42008C0340209202UL,
0x209188240001000UL,
0x400408A884001800UL,
0x110400A6080400UL,
0x1840060A44020800UL,
0x90080104000041UL,
0x201011000808101UL,
0x1A2208080504F080UL,
0x8012020600211212UL,
0x500861011240000UL,
0x180806108200800UL,
0x4000020E01040044UL,
0x300000261044000AUL,
0x802241102020002UL,
0x20906061210001UL,
0x5A84841004010310UL,
0x4010801011C04UL,
0xA010109502200UL,
0x04A02012000UL,
0x500201010098B028UL,
0x8040002811040900UL,
0x28000010020204UL,
0x6000020202D0240UL,
0x8918844842082200UL,
0x4010011029020020UL
        };

        Dictionary<int, string> MoveType = new()
        {
            {quiet_move, "quiet_move" },
            {double_pawn_push, "double_pawn_push" },
            {king_castle, "king_castle" },
            {queen_castle, "queen_castle" },
            {capture, "capture" },
            {ep_capture, "ep_capture" },
            {knight_promo, "knight_promo" },
            {bishop_promo, "bishop_promo" },
            {rook_promo, "rook_promo" },
            {queen_promo, "queen_promo" },
            {knight_promo_capture, "knight_promo_capture" },
            {bishop_promo_capture, "bishop_promo_capture" },
            {rook_promo_capture, "rook_promo_capture" },
            {queen_promo_capture, "queen_promo_capture" },


        };

        Dictionary<char, int> char_pieces = new()
        {
            { 'P', Piece.P },
            { 'N', Piece.N },
            { 'B', Piece.B },
            { 'R', Piece.R },
            { 'Q', Piece.Q },
            { 'K', Piece.K },
            { 'p', Piece.p },
            { 'n', Piece.n },
            { 'b', Piece.b },
            { 'r', Piece.r },
            { 'q', Piece.q },
            { 'k', Piece.k },
            

        };


        Dictionary<int, char> ascii_pieces = new()
        {
            { Piece.P, 'P'  },
            {Piece.N,  'N' },
            {  Piece.B , 'B'},
            { Piece.R , 'R' },
            {Piece.Q ,'Q' },
            { Piece.K, 'K' },
            {Piece.p,  'p' },
            { Piece.n,  'n' },
            { Piece.b, 'b'},
            { Piece.r, 'r' },
            { Piece.q , 'q' },
            {Piece.k , 'k'},


        };
        public class Piece // 0~5 white 6~11 black
        {
            public const int P = 0;
            public const int N = 1;
            public const int B = 2;
            public const int R = 3;
            public const int Q = 4;
            public const int K = 5;
            public const int p = 6;
            public const int n = 7;
            public const int b = 8;
            public const int r = 9;
            public const int q = 10;
            public const int k = 11;

        }
        public class Side
        {
            public const int White = 0;
            public const int Black = 1;
            public const int Both = 2;

        }

        struct Move
        {
            public int From;
            public int To;
            public int Type;
            public int Piece;

            public Move(int from, int to, int type, int piece)
            {
                From = from; To = to; Type = type; Piece = piece;
            }
        }

        class Board
        {
            public ulong[] bitboards = new ulong[12];
            public ulong[] occupancies = new ulong[3];
            public int[] mailbox = new int[64];
            public int side;
            public int enpassent = (int)Square.no_sq;
            public ulong castle;



        }
        //enum Piece
        //{
        //    P, N, B, R, Q, K, p, n, b, r, q, k
        //}
        //enum Side
        //{
        //    White, Black, Both

        //}
        int Peft_DEPTH = 0;

        void InitializeBetweenTable()
        {
            for (int a = 0; a < 64; a++)
            {
                for (int b = 0; b < 64; b++)
                {
                    betweenTable[a, b] = Calcbetween(a, b);
                }
            }
        }
        static void Main(string[] args)
        {
            
            Board Main_Board = new Board();
            Engine genMove = new();

            List<Move> moveList = new List<Move>();
            Stopwatch st = new Stopwatch();
            st.Start();
            genMove.InitializeBetweenTable();
            genMove.InitializeLeaper();
            genMove.init_sliders_attacks(1);
            genMove.init_sliders_attacks(0);
            st.Stop();
            float time = (float)st.Elapsed.TotalNanoseconds;
            Console.WriteLine("initialization stoped : " + time / 1000000 + "MS");
            ulong occupancy = 0UL;

            PrintBitboard(genMove.between((int)Square.a1, (int)Square.h8));
            

            //genMove.parse_fen("8/8/8/4R3/3B4/8/8/8 w - - ");
            //genMove.parse_fen("7k/1pPp1pp1/2Q2q1p/2pPp3/1Nb5/2B1N1R1/1rP2KPP/8 w - - 0 1", board);
            genMove.parse_fen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1", Main_Board);
            

            genMove.PrintBoards(Main_Board);
            genMove.print_mailbox(Main_Board.mailbox);

            genMove.Generate_Legal_Moves(ref moveList, Main_Board);

            genMove.PrintLegalMoves(moveList);
            genMove.Peft_DEPTH = int.Parse(Console.ReadLine());

            //Stopwatch st = new Stopwatch();
            st.Reset();
            st.Start();

            ulong nodes = genMove.perft(genMove.Peft_DEPTH, Main_Board);
            time = (float)st.Elapsed.TotalNanoseconds;
            float timeMS = time / 1000000;
            float NPS = nodes / ((timeMS / 1000));
            Console.WriteLine((timeMS / 1000));

            Console.WriteLine("Nodes: " + nodes + " NPS: " + NPS + " time(MS) " + timeMS);
            //Console.WriteLine(nodes);
            st.Stop();
            
            //Console.WriteLine((time / 1000000000));

            //if(((time / 1000000000)) != 0)
            //{
            //    Console.WriteLine(nodes / ((time / 1000000000)));
            //}
            //else
            //{
            //    Console.WriteLine("unmeasurable");
            //}
            

            //while(true)
            //{


            //    int num = int.Parse(Console.ReadLine());
            //    int lastEp = Main_Board.enpassent;
            //    ulong lastCastle = Main_Board.castle;
            //    int lastside = Main_Board.side;
            //    int captured_piece = Main_Board.mailbox[moveList[num].To];
            //    Move lmove = moveList[num];
            //    genMove.MakeMove(ref Main_Board, moveList[num]);
            //    moveList.Clear();
            //    genMove.Generate_Legal_Moves(ref moveList, Main_Board);

            //    genMove.PrintBoards(Main_Board);

            //    genMove.print_mailbox(Main_Board.mailbox);

            //    Console.WriteLine("White");
            //    PrintBitboard(Main_Board.occupancies[Side.White]);
            //    Console.WriteLine("Black");
            //    PrintBitboard(Main_Board.occupancies[Side.Black]);
            //    Console.WriteLine("Both");
            //    PrintBitboard(Main_Board.occupancies[Side.Both]);
            //    genMove.PrintLegalMoves(moveList);


            //    //Console.WriteLine("======= unmake move =======");


            //    //genMove.UnmakeMove(ref Main_Board, lmove, captured_piece);
            //    //Main_Board.enpassent = lastEp;
            //    //Main_Board.castle = lastCastle;
            //    //Main_Board.side = lastside;


            //    //genMove.PrintBoards(Main_Board);

            //    //genMove.print_mailbox(Main_Board.mailbox);

            //    //Console.WriteLine("White");
            //    //PrintBitboard(Main_Board.occupancies[Side.White]);
            //    //Console.WriteLine("Black");
            //    //PrintBitboard(Main_Board.occupancies[Side.Black]);
            //    //Console.WriteLine("Both");
            //    //PrintBitboard(Main_Board.occupancies[Side.Both]);
            //    ////genMove.PrintLegalMoves(moveList);
            //}

            //PrintBitboard(genMove.get_attacked_squares(Side.White, board));
            //PrintBitboard(genMove.between((int)Square.a1, (int)Square.h8));


        }
        ulong perft(int depth, Board board)
        {
            
            List<Move> movelist = new();
            int n_moves, i;
            ulong nodes = 0;

            if (depth == 0) return 1UL;

            Generate_Legal_Moves(ref movelist, board);
            n_moves = movelist.Count;

            for(i =0; i < n_moves; i++)
            {
                
                int lastEp = board.enpassent;
                ulong lastCastle = board.castle;
                int lastside = board.side;
                int captured_piece = board.mailbox[movelist[i].To];

                MakeMove(ref board, movelist[i]);
                ulong added_nodes = perft(depth - 1, board);
                if(depth == Peft_DEPTH)
                {
                    printMove(movelist[i]);

                    Console.Write(":" + added_nodes + "\n");
                }
                
                nodes += added_nodes;
                UnmakeMove(ref board, movelist[i], captured_piece);
                board.enpassent = lastEp;
                board.castle = lastCastle;
                board.side = lastside;

            }


            return nodes;
        }
        ulong Calcbetween(int a, int b)
        {
            ulong between = 0;


            int xDiff = getFile(a) - getFile(b);
            int yDiff = getRank(a) - getRank(b);


            int totalSteps;
            if (xDiff == 0)
            {
                totalSteps = Math.Abs(yDiff);
            }
            else
            {
                totalSteps = Math.Abs(xDiff);
            }

            if (totalSteps == 0) return 0;

            float testx = -xDiff / (float)totalSteps;
            float testy = yDiff / (float)totalSteps;

            //Console.WriteLine(xStep + " ," + yStep);
            if (testx > 1 || testx < -1 || testy > 1 || testy < -1) return 0;
            if (testx == 0 && testy == 0) return 0;
            if ((testx % 1 != 0) || (testy % 1 != 0)) return 0;


            int xStep = (int)testx;
            int yStep = (int)testy;
            int pos = a;
            int howmuch = 0;
            //Console.WriteLine(pos);
            //Set_bit(ref between, pos);
            while (pos != b)
            {
                //CoordinatesToChessNotation
                pos += xStep;
                pos += yStep * 8;
                Set_bit(ref between, pos);

                if (howmuch > 10) Console.WriteLine(CoordinatesToChessNotation(a) + "," + CoordinatesToChessNotation(b) + " " + xStep + "," + yStep + " " + totalSteps);
                howmuch++;
                //Console.WriteLine(pos);

            }
            between &= ~(1UL << b);
            return between;
        }

        ulong between(int a, int b)
        {
            return betweenTable[a, b];
        }
        void PrintLegalMoves(List<Move> moveList)
        {
            int num = 0;
            foreach(Move move in  moveList)
            {
                Console.Write(num + CoordinatesToChessNotation(move.From) + CoordinatesToChessNotation(move.To) );
                if (move.Type == queen_promo || move.Type == queen_promo_capture) Console.Write("q");
                if (move.Type == rook_promo || move.Type == rook_promo_capture) Console.Write("r");
                if (move.Type == bishop_promo || move.Type == bishop_promo_capture) Console.Write("b");
                if (move.Type == knight_promo || move.Type == knight_promo_capture) Console.Write("n");

                Console.Write(": 1 \n " );

                num++;
            }
        }

        void printMove(Move move)
        {
            Console.Write(CoordinatesToChessNotation(move.From) + CoordinatesToChessNotation(move.To));
            if (move.Type == queen_promo || move.Type == queen_promo_capture) Console.Write("q");
            if (move.Type == rook_promo || move.Type == rook_promo_capture) Console.Write("r");
            if (move.Type == bishop_promo || move.Type == bishop_promo_capture) Console.Write("b");
            if (move.Type == knight_promo || move.Type == knight_promo_capture) Console.Write("n");
        }
        int getSide(int piece)
        {
            return (piece > 5) ? Side.Black : Side.White;
                //Piece
        }
        void MakeMove(ref Board board, Move move)
        {
            board.enpassent = (int)Square.no_sq;
            int side = board.side;
            // change castling flag
            if (get_piece(move.Piece, Side.White) == Piece.K) //if king moved
            {
                if (side == Side.White)
                {
                    board.castle &= ~WhiteKingCastle;
                    board.castle &= ~WhiteQueenCastle;

                }
                else
                {
                    board.castle &= ~BlackKingCastle;
                    board.castle &= ~BlackQueenCastle;
                }
            }
            if (get_piece(move.Piece, Side.White) == Piece.R) //if rook moved
            {
                if (side == Side.White)
                {
                    if((board.castle & WhiteQueenCastle) != 0 && move.From == (int)Square.a1) // no q castle
                    {
                        board.castle &= ~WhiteQueenCastle;
                    }
                    else if((board.castle & WhiteKingCastle) != 0 && move.From == (int)Square.h1) // no k castle
                    {
                        board.castle &= ~WhiteKingCastle;
                    }
                    


                }
                else
                {
                    if ((board.castle & BlackQueenCastle) != 0 && move.From == (int)Square.a8) // no q castle
                    {
                        board.castle &= ~BlackQueenCastle;
                    }
                    else if ((board.castle & BlackKingCastle) != 0 && move.From == (int)Square.h8) // no k castle
                    {
                        board.castle &= ~BlackKingCastle;
                    }
                }
            }
            if (move.Type == quiet_move || move.Type == double_pawn_push)
            {
                
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[move.Piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To); 

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = move.Piece;

                //update enpassent square
                if(move.Type == double_pawn_push)
                {
                    if(side == Side.White)
                    {
                        board.enpassent =move.To + 8;
                    }
                    else
                    {
                        board.enpassent = move.To - 8;
                    }
                    
                }
                
            }
            else if (move.Type == capture)
            {

                //update piece bitboard
                int captured_piece = board.mailbox[move.To];


                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[move.Piece] |= (1UL << move.To);

                board.bitboards[captured_piece] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = move.Piece;

            }
            else if(move.Type == king_castle)
            {
                //update castling right & find rook square
                
                
                int rookSquare;
                if(side == Side.White)
                {
                    rookSquare = (int)Square.h1;
                    //board.castle &= ~WhiteKingCastle;
                }
                else
                {
                    rookSquare = (int)Square.h8;
                    //board.castle &= ~BlackKingCastle;

                }


                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[move.Piece] |= (1UL << move.To);

                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare - 2));


                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                board.occupancies[side] &= ~(1UL << rookSquare);
                board.occupancies[side] |= (1UL << (rookSquare - 2));

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);

                board.occupancies[Side.Both] &= ~(1UL << rookSquare);
                board.occupancies[Side.Both] |= (1UL << (rookSquare - 2));
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = move.Piece;

                board.mailbox[rookSquare] = -1;
                board.mailbox[rookSquare - 2] = get_piece(Piece.r, side);


            }
            else if (move.Type == queen_castle)
            {
                //update castling right & find rook square


                int rookSquare;
                if (side == Side.White)
                {
                    rookSquare = (int)Square.a1;
                    //board.castle &= ~WhiteKingCastle;
                }
                else
                {
                    rookSquare = (int)Square.a8;
                    //board.castle &= ~BlackKingCastle;

                }
                Console.WriteLine(CoordinatesToChessNotation(rookSquare));

                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[move.Piece] |= (1UL << move.To);

                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare + 3));


                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                board.occupancies[side] &= ~(1UL << rookSquare);
                board.occupancies[side] |= (1UL << (rookSquare + 3));

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);

                board.occupancies[Side.Both] &= ~(1UL << rookSquare);
                board.occupancies[Side.Both] |= (1UL << (rookSquare + 3));
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = move.Piece;

                board.mailbox[rookSquare] = -1;
                board.mailbox[rookSquare + 3] = get_piece(Piece.r, side);


            }
            else if (move.Type == queen_promo)
            {
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.q, side)] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.q, side);

            }
            else if(move.Type == rook_promo)
            {
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.r, side);
            }
            else if (move.Type == bishop_promo)
            {
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.b, side)] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.b, side);
            }
            else if (move.Type == knight_promo)
            {
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.n, side)] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);
                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.n, side);

            }
            else if (move.Type == queen_promo_capture)
            {
                int captured_piece = board.mailbox[move.To];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.q, side)] |= (1UL << move.To);

                board.bitboards[captured_piece] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);

                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.q, side);
            }
            else if (move.Type == rook_promo_capture)
            {
                int captured_piece = board.mailbox[move.To];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << move.To);

                board.bitboards[captured_piece] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);

                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.r, side);
            }
            else if (move.Type == bishop_promo_capture)
            {
                int captured_piece = board.mailbox[move.To];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.b, side)] |= (1UL << move.To);

                board.bitboards[captured_piece] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);

                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.b, side);
            }
            else if (move.Type == knight_promo_capture)
            {
                int captured_piece = board.mailbox[move.To];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[get_piece(Piece.n, side)] |= (1UL << move.To);

                board.bitboards[captured_piece] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.To);

                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = get_piece(Piece.n, side);
            }
            else if (move.Type == ep_capture)
            {
                int capture_square;
                if (side == Side.White)
                {
                    capture_square = move.To + 8;
                }
                else
                {
                    capture_square = move.To - 8;
                }


                int captured_piece = board.mailbox[capture_square];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.From);
                board.bitboards[move.Piece] |= (1UL << move.To);

                board.bitboards[captured_piece] &= ~(1UL << capture_square);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.From);
                board.occupancies[side] |= (1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] &= ~(1UL << capture_square);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << capture_square);
                board.occupancies[Side.Both] |= (1UL << move.To);

                //update mailbox
                board.mailbox[move.From] = -1;
                board.mailbox[move.To] = move.Piece;
                board.mailbox[capture_square] = -1;
            }



            board.side = 1 - board.side;
        }

        void UnmakeMove(ref Board board, Move move, int captured_piece)
        {

            int side = 1 - board.side;
            // change castling flag

            if (move.Type == quiet_move || move.Type == double_pawn_push)
            {
                //Console.WriteLine("q");
                //Console.WriteLine(CoordinatesToChessNotation(move.From) + "," + CoordinatesToChessNotation(move.To));
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == capture)
            {

                //update piece bitboard
                //int captured_piece = board.mailbox[move.To];
                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);
                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == king_castle)
            {
                //update castling right & find rook square


                int rookSquare;
                if (side == Side.White)
                {
                    rookSquare = (int)Square.h1;
                    //board.castle &= ~WhiteKingCastle;
                }
                else
                {
                    rookSquare = (int)Square.h8;
                    //board.castle &= ~BlackKingCastle;

                }


                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare - 2);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare));


                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                board.occupancies[side] &= ~(1UL << rookSquare - 2);
                board.occupancies[side] |= (1UL << (rookSquare));

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);

                board.occupancies[Side.Both] &= ~(1UL << rookSquare - 2);
                board.occupancies[Side.Both] |= (1UL << (rookSquare));
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

                board.mailbox[rookSquare - 2] = -1;
                board.mailbox[rookSquare] = get_piece(Piece.r, side);


            }
            else if (move.Type == queen_castle)
            {
                //update castling right & find rook square


                int rookSquare;
                if (side == Side.White)
                {
                    rookSquare = (int)Square.a1;
                    //board.castle &= ~WhiteKingCastle;
                }
                else
                {
                    rookSquare = (int)Square.a8;
                    //board.castle &= ~BlackKingCastle;

                }
                //Console.WriteLine(CoordinatesToChessNotation(rookSquare));

                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare +3);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare));


                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                board.occupancies[side] &= ~(1UL << rookSquare + 3);
                board.occupancies[side] |= (1UL << (rookSquare));

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);

                board.occupancies[Side.Both] &= ~(1UL << rookSquare + 3);
                board.occupancies[Side.Both] |= (1UL << (rookSquare));
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

                board.mailbox[rookSquare + 3] = -1;
                board.mailbox[rookSquare] = get_piece(Piece.r, side);
            }
            else if (move.Type == queen_promo)
            {
                //update piece bitboard
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.q, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == rook_promo)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == bishop_promo)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.b, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == knight_promo)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.n, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == queen_promo_capture)
            {
                //int captured_piece = board.mailbox[move.To];
                //update piece bitboard


                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.q, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;








               

            }
            else if (move.Type == rook_promo_capture)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == bishop_promo_capture)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.b, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == knight_promo_capture)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.n, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == ep_capture)
            {
                int capture_square;
                int captured_pawn = get_piece(Piece.p, 1 - side);
                if (side == Side.White)
                {
                    capture_square = move.To + 8;
                }
                else
                {
                    capture_square = move.To - 8;
                }


                //int captured_piece = board.mailbox[capture_square];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[captured_pawn] |= (1UL << capture_square);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << capture_square);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << capture_square);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.From] = move.Piece;
                board.mailbox[move.To] = -1;
                board.mailbox[capture_square] = captured_pawn;
            }



            board.side = 1 - board.side;
        }
        void Generate_Legal_Moves(ref List<Move> MoveList, Board board)
        {
            int WK_Square = get_ls1b(board.bitboards[Piece.K]);
            int BK_Square = get_ls1b(board.bitboards[Piece.k]);

            int my_king = (board.side == Side.White) ? WK_Square : BK_Square;
            //ulong Attacked_square = get_attacked_squares(oppSide, board, (board.occupancies[Side.Both] & ~KingBB));
            //Console.WriteLine((board.side == Side.White));
            List<ulong> pin_ray = new();
            List<ulong> pinned_piece = new();


            ulong check_attackers = 0;
            //ulong attacked_square = is_square_attacked
            Detect_pinned_pieces(my_king, ref pinned_piece, ref pin_ray, board);


            //Console.WriteLine(pinned_piece.Count);
            //PrintBitboard(pin_ray[0]);
            Detect_Check_Attackers(my_king, ref check_attackers, board);
            //Console.WriteLine(count_bits(check_attackers));

            //PrintBitboard(check_attackers); 
            ulong move_mask = 0xFFFFFFFFFFFFFFFF;

            //PrintBitboard(move_mask);
            ulong capture_mask = 0xFFFFFFFFFFFFFFFF;
            //print_bitboard(move_mask);
            if(count_bits(check_attackers) == 1) // single check
            {
                move_mask = between(my_king, get_ls1b(check_attackers));

                capture_mask = check_attackers;

                if (board.enpassent != (int)Square.no_sq)
                {
                    capture_mask |= (1UL << board.enpassent);
                }
                
            }
            //PrintBitboard(move_mask | capture_mask);
            
            //Console.WriteLine(count_bits(check_attackers));
            //print_bitboard(pinned_ray);
            //for (int i = 0; i < pinned_piece.Count; i++)
            //{
            //    PrintBitboard(pinned_piece[i]); 
            //    PrintBitboard(pin_ray[i]); 
            //}

            Generate_Pawn_Moves(ref MoveList, board, check_attackers, move_mask, capture_mask, pin_ray, pinned_piece);
            Generate_Knight_Moves(ref MoveList, board, check_attackers, move_mask | capture_mask, pin_ray, pinned_piece);
            Generate_Bishop_Moves(ref MoveList, board, check_attackers, move_mask | capture_mask, pin_ray, pinned_piece);
            Generate_Rook_Moves(ref MoveList, board, check_attackers, move_mask | capture_mask, pin_ray, pinned_piece);
            Generate_Queen_Moves(ref MoveList, board, check_attackers , move_mask |capture_mask, pin_ray, pinned_piece);
            Generate_King_Moves(ref MoveList, board, check_attackers);

        }

        void Detect_Check_Attackers(int King, ref ulong attackers, Board board)
        {

            int side = board.side;
            int oppSide = 1 - board.side;

            ulong oppKnight = ((side == Side.White) ? board.bitboards[Piece.n] : board.bitboards[Piece.N]);
            ulong oppBishop = ((side == Side.White) ? board.bitboards[Piece.b] : board.bitboards[Piece.B]);
            ulong oppRook = ((side == Side.White) ? board.bitboards[Piece.r] : board.bitboards[Piece.R]);
            ulong oppQueen = ((side == Side.White) ? board.bitboards[Piece.q] : board.bitboards[Piece.Q]);
            ulong oppPawn = ((side == Side.White) ? board.bitboards[Piece.p] : board.bitboards[Piece.P]);

            ulong check_attackers = 0;
            check_attackers |= Knight_attacks[King] & oppKnight;

            check_attackers |= get_bishop_attacks(King, board.occupancies[Side.Both]) & oppBishop;

            check_attackers |= get_rook_attacks(King, board.occupancies[Side.Both]) & oppRook;

            check_attackers |= get_queen_attacks(King, board.occupancies[Side.Both]) & oppQueen;

            check_attackers |= pawn_attacks[side, King] & oppPawn;

            //PrintBitboard(pawn_attacks[side, King]);
            attackers = check_attackers;
            
        }
        void Detect_pinned_pieces(int King, ref List<ulong> pinned_piece, ref List<ulong> pin_ray, Board board)
        {
            int side = board.side;
            int oppSide = 1 - board.side;


            //Console.WriteLine(King);
            ulong poss_pinning_hor;
            ulong poss_pinning_dia;
            if (side == Side.White)//white
            {
                poss_pinning_hor = board.bitboards[Piece.r] | board.bitboards[Piece.q];
                poss_pinning_dia = board.bitboards[Piece.b] | board.bitboards[Piece.q];
            }
            else
            {
                poss_pinning_hor = board.bitboards[Piece.R] | board.bitboards[Piece.Q];
                poss_pinning_dia = board.bitboards[Piece.B] | board.bitboards[Piece.Q];
            }
            

            ulong pinned_ray = get_queen_attacks(King, board.occupancies[oppSide]);


            ulong possible_pinned_piece = pinned_ray & board.occupancies[side];

            ulong possible_pinning_piece = pinned_ray & (poss_pinning_hor | poss_pinning_dia);



            //PrintBitboard(possible_pinning_piece);

            for (; possible_pinning_piece != 0;)
            {
                int pos = get_ls1b(possible_pinning_piece);

                int posX = getFile(pos);
                int posY = getRank(pos);
                int kingX = getFile(King);
                int kingY = getRank(King);

                ulong KingLine = between(pos, King);

                if (KingLine == 0UL) continue;
                if(posX == kingX || posY == kingY) //straight
                {
                    if(((1UL << pos) & poss_pinning_hor) == 0)
                    {
                        Pop_bit(ref possible_pinning_piece, pos);
                        continue;
                    }
                }
                else //diagonal
                {
                    if (((1UL << pos) & poss_pinning_dia) == 0)
                    {
                        Pop_bit(ref possible_pinning_piece, pos);
                        continue;
                    }
                }
                //PrintBitboard(KingLine);
                //ulong pinned_piece = 0;
                //Console.WriteLine(count_bits(KingLine));
                if (count_bits(KingLine & board.occupancies[side]) == 1) // possiibly pinned
                {
                    pin_ray.Add(KingLine | (1UL << pos));
                    pinned_piece.Add(KingLine & board.occupancies[side]);
                    //PrintBitboard(pinned_piece[pinned_piece.Count - 1]);

                }
                //PrintBitboard(between(pos, King) | (1UL << pos));

                Pop_bit(ref possible_pinning_piece, pos);
            }
            //between
            //ulong pinned_pieces = pinned_ray & board.occupancies[Side.Both];

            //pinned_ray = pinned_pieces;
            //List<int> possible_pinned_piece;
            //List<int> possible_pinning_piece;



        }
        void Generate_Pawn_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, ulong capture_mask, List<ulong> pin_ray, List<ulong> pinned_piece)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;


            int side = board.side;

            
            ulong PawnBB = (board.side == Side.White) ? board.bitboards[Piece.P] : board.bitboards[Piece.p];
            List<int> pinned = new();
            List<int> pinned_Loc = new();
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((PawnBB & pinned_piece[i]) != 0) // found pinned bishop
                {
                    
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));

                    //Console.WriteLine(CoordinatesToChessNotation(get_ls1b(pinned_piece[i])));
                }

            }
            for (; PawnBB != 0;)
            {
                int From = get_ls1b(PawnBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
                ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinned[pinnedloc]];
                    //PrintBitboard(pin_mask);
                }
                ulong BB = 1UL << From;
                if((board.side == Side.White) ? (From >= (int)Square.a7 && From <= (int)Square.h7) : (From >= (int)Square.a2 && From <= (int)Square.h2))
                {
                    // =======promotion======= //

                    ulong pawnPromo = (((side == Side.White) ? (BB >> 8) : (BB << 8)) & ~board.occupancies[Side.Both]) & move_mask & pin_mask;
                    //bool isPossible = pawnOnePush != 0;


                    for (; pawnPromo != 0; )
                    {
                        //Console.WriteLine(pawnPromo);
                        int To = get_ls1b(pawnPromo);
                        MoveList.Add(new Move(From, To, knight_promo, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, bishop_promo, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, rook_promo, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, queen_promo, get_piece(Piece.p, side)));
                        Pop_bit(ref pawnPromo, To);
                        //Console.WriteLine(pawnPromo);

                    }
                    // =======promo_capture======= //
                    ulong pawn_capture_mask = pawn_attacks[board.side, From];
                    ulong pawn_capture = ((board.side == Side.White) ? pawn_capture_mask & board.occupancies[Side.Black] : pawn_capture_mask & board.occupancies[Side.White]) &( move_mask | capture_mask)& pin_mask  ;

                    for (; pawn_capture != 0;)
                    {
                        int To = get_ls1b(pawn_capture);
                        MoveList.Add(new Move(From, To, knight_promo_capture, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, bishop_promo_capture, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, rook_promo_capture, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, queen_promo_capture, get_piece(Piece.p, side)));
                        Pop_bit(ref pawn_capture, To);
                    }


                }
                else
                {
                    // =======pawn one square push======= //

                    ulong pawnOnePush = (((side == Side.White) ? (BB >> 8) : (BB << 8)) & ~board.occupancies[Side.Both]) & move_mask & pin_mask;
                    bool isPossible = pawnOnePush != 0;


                    for (; pawnOnePush != 0;)
                    {
                        int To = get_ls1b(pawnOnePush);
                        MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.p, side)));
                        Pop_bit(ref pawnOnePush, To);
                    }

                    // =======pawn two square push======= //

                    ulong pawnTwoPush = 0;
                    if ((board.side == Side.White) ? (From >= (int)Square.a2 && From <= (int)Square.h2) : (From >= (int)Square.a7 && From <= (int)Square.h7))//pawn on second rank
                    {
                        if (isPossible || check_num == 1)//one push possible
                        {
                            pawnTwoPush = (((side == Side.White) ? (BB >> 16) : (BB << 16)) & ~board.occupancies[Side.Both]) & move_mask & pin_mask;
                        }
                    }
                    if (pawnTwoPush != 0)
                    {
                        for (; pawnTwoPush != 0;)
                        {
                            int To = get_ls1b(pawnTwoPush);
                            MoveList.Add(new Move(From, To, double_pawn_push, get_piece(Piece.p, side)));
                            Pop_bit(ref pawnTwoPush, To);
                        }
                    }

                    // =======pawn capture======= //
                    ulong pawn_capture_mask = pawn_attacks[board.side, From];
                    ulong pawn_capture = ((board.side == Side.White) ? pawn_capture_mask & board.occupancies[Side.Black] : pawn_capture_mask & board.occupancies[Side.White]) & (move_mask | capture_mask) & pin_mask;

                    for (; pawn_capture != 0;)
                    {
                        int To = get_ls1b(pawn_capture);
                        MoveList.Add(new Move(From, To, capture, get_piece(Piece.p, side)));
                        Pop_bit(ref pawn_capture, To);
                    }

                    // =======pawn Enpassent =======//
                    ulong enpassent = 0;
                    if (board.enpassent != (int)Square.no_sq) // enpassent possible
                    {
                        
                        enpassent = (pawn_capture_mask & (1UL << board.enpassent)) & capture_mask & pin_mask;


                    }

                    for (; enpassent != 0;)
                    {
                        int To = get_ls1b(enpassent);

                        int pawnToCapture = 0;
                        if(side == Side.White)
                        {
                            pawnToCapture = To + 8;
                        }
                        else
                        {
                            pawnToCapture = To - 8;
                        }
                        int King = (side == Side.White) ? get_ls1b(board.bitboards[Piece.K]) : get_ls1b(board.bitboards[Piece.k]);
                        bool isAttacked =  is_square_attacked(King, 1 - side, board, board.occupancies[Side.Both] & ~(1UL << From) & ~(1UL << pawnToCapture));
                        
                        if(!isAttacked)
                        {
                            MoveList.Add(new Move(From, To, ep_capture, get_piece(Piece.p, side)));
                        }
                       
                        Pop_bit(ref enpassent, To);
                    }
                    
                }
                Pop_bit(ref PawnBB, From);
            }
        }
        int get_piece(int piece, int col)
        {
            if(col == Side.White)
            {
                //6이상 흑
                if(piece >= 6)
                {
                    return piece - 6;
                }
                return piece;
            }
            else
            {
                if (piece >= 6)
                {
                    return piece;
                    
                }
                return piece + 6;
            }
        }
        void Generate_Knight_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, List<ulong> pin_ray, List<ulong> pinned_piece)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;
            List<int> pinned = new();
            List<int> pinned_Loc = new();
            ulong KnightBB = (board.side == Side.White) ? board.bitboards[Piece.N] : board.bitboards[Piece.n];
            //ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((KnightBB & pinned_piece[i]) != 0) // found pinned bishop
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }

            //PrintBitboard(KnightBB);
            for (; KnightBB != 0;)
            {
                int From = get_ls1b(KnightBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
                //Console.WriteLine("a" + pinned_Loc.Count);
                //Console.WriteLine("a" + pinnedloc);

                //Console.WriteLine(CoordinatesToChessNotation(From));
                if (pinnedloc != -1)
                {
                    Pop_bit(ref KnightBB, From);
                    continue;
                }
                //PrintBitboard(1UL << From);
                ulong KnightMove = (Knight_attacks[From] & ~board.occupancies[side]) & move_mask;
                //PrintBitboard((Knight_attacks[From] & ~board.occupancies[side]) );

                for (; KnightMove != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(KnightMove);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        MoveList.Add(new Move(From, To, capture, get_piece(Piece.n, side)));
                    }
                    else
                    {
                        MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.n, side)));
                    }

                    Pop_bit(ref KnightMove, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref KnightBB, From);
            }
        }
        void Generate_Bishop_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, List<ulong> pin_ray, List<ulong> pinned_piece)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;

            ulong BishopBB = (board.side == Side.White) ? board.bitboards[Piece.B] : board.bitboards[Piece.b];
            List<int> pinned = new();
            List<int> pinned_Loc = new();

            ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((BishopBB & pinned_piece[i]) != 0) // found pinned bishop
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }
            for (; BishopBB != 0;)
            {
                int From = get_ls1b(BishopBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinnedloc];
                }
                //PrintBitboard(1UL << From);
                ulong BishopMoves = (get_bishop_attacks(From, board.occupancies[Side.Both]) & ~board.occupancies[side]) & move_mask & pin_mask;
                //PrintBitboard(BishopMoves);

                for (; BishopMoves != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(BishopMoves);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        MoveList.Add(new Move(From, To, capture, get_piece(Piece.b, side)));
                    }
                    else
                    {
                        MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.b, side)));
                    }

                    Pop_bit(ref BishopMoves, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref BishopBB, From);
            }
        }
        void Generate_Rook_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, List<ulong> pin_ray, List<ulong> pinned_piece)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;

            ulong RookBB = (board.side == Side.White) ? board.bitboards[Piece.R] : board.bitboards[Piece.r];
            List<int> pinned = new();
            List<int> pinned_Loc = new();

            ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((RookBB & pinned_piece[i]) != 0) // found pinned rook
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }

            for (; RookBB != 0;)
            {
                int From = get_ls1b(RookBB);
                //PrintBitboard(1UL << From);
                int pinnedloc = pinned_Loc.IndexOf(From);
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinnedloc];
                }
                ulong RookMoves = (get_rook_attacks(From, board.occupancies[Side.Both]) & ~board.occupancies[side]) & move_mask & pin_mask;
                //PrintBitboard(BishopMoves);

                for (; RookMoves != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(RookMoves);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        MoveList.Add(new Move(From, To, capture, get_piece(Piece.r, side)));
                    }
                    else
                    {
                        MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.r, side)));
                    }

                    Pop_bit(ref RookMoves, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref RookBB, From);
            }
        }

        void Generate_Queen_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, List<ulong> pin_ray, List<ulong> pinned_piece)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;

            ulong QueenBB = (board.side == Side.White) ? board.bitboards[Piece.Q] : board.bitboards[Piece.q];
            List<int> pinned = new();
            List<int> pinned_Loc = new();
            ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((QueenBB & pinned_piece[i]) != 0) // found pinned queen
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }
            for (; QueenBB != 0;)
            {
                int From = get_ls1b(QueenBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinnedloc];
                }
                //PrintBitboard(1UL << From);
                ulong QueenMoves = (get_queen_attacks(From, board.occupancies[Side.Both]) & ~board.occupancies[side]) & move_mask & pin_mask;
                //PrintBitboard(BishopMoves);

                for (; QueenMoves != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(QueenMoves);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        MoveList.Add(new Move(From, To, capture, get_piece(Piece.q, side)));
                    }
                    else
                    {
                        MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.q, side)));
                    }

                    Pop_bit(ref QueenMoves, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref QueenBB, From);
            }
        }

        void Generate_King_Moves(ref List<Move> MoveList, Board board, ulong attackers)
        {
            //int check_num = count_bits(attackers);
            int side = board.side;

            int oppSide = 1 - side;

            ulong KingBB = (board.side == Side.White) ? board.bitboards[Piece.K] : board.bitboards[Piece.k];
            int From = get_ls1b(KingBB);
            
            ulong Attacked_square = get_attacked_squares(oppSide, board, (board.occupancies[Side.Both] & ~KingBB));

            //PrintBitboard(Attacked_square);
            
            //PrintBitboard(1UL << From);

            ulong KingMoves = King_attacks[From] & ~board.occupancies[side] & ~Attacked_square;
            //PrintBitboard(BishopMoves);

            for (; KingMoves != 0;)
            {
                //Console.WriteLine(pawnPromo);
                
                int To = get_ls1b(KingMoves);
                if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                {
                    MoveList.Add(new Move(From, To, capture, get_piece(Piece.k, side)));
                }
                else
                {
                    MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.k, side)));
                }

                Pop_bit(ref KingMoves, To);
                //Console.WriteLine(pawnPromo);

            }

            if(side == Side.White)
            {
                if((board.castle & WhiteKingCastle) != 0) // kingside castling
                {
                    if ((board.occupancies[Side.Both] & (1UL << (int)Square.f1)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.g1)) == 0)
                    {
                        if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.f1)) == 0 && (Attacked_square & (1UL << (int)Square.g1)) == 0) // not check
                        {

                            MoveList.Add(new Move((int)Square.e1, (int)Square.g1, king_castle, get_piece(Piece.k, side)));
                        }
                        
                    }

                }
                if((board.castle & WhiteQueenCastle) != 0)
                {
                    if ((board.occupancies[Side.Both] & (1UL << (int)Square.d1)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.c1)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.b1)) == 0)
                    {
                        if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.d1)) == 0 && (Attacked_square & (1UL << (int)Square.c1)) == 0) // not check
                        {
                            MoveList.Add(new Move((int)Square.e1, (int)Square.c1, queen_castle, get_piece(Piece.k, side)));
                        }
                    }

                }
            }
            else
            {
                if ((board.castle & BlackKingCastle) != 0) // kingside castling
                {
                    if ((board.occupancies[Side.Both] & (1UL << (int)Square.f8)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.g8)) == 0)
                    {
                        if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.f8)) == 0 && (Attacked_square & (1UL << (int)Square.g8)) == 0) // not check
                        {

                            MoveList.Add(new Move((int)Square.e8, (int)Square.g8, king_castle, get_piece(Piece.k, side)));
                        }

                    }

                }
                if ((board.castle & BlackQueenCastle) != 0)
                {
                    if ((board.occupancies[Side.Both] & (1UL << (int)Square.d8)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.c8)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.b8)) == 0)
                    {
                        if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.d8)) == 0 && (Attacked_square & (1UL << (int)Square.c8)) == 0) // not check
                        {
                            MoveList.Add(new Move((int)Square.e8, (int)Square.c8, queen_castle, get_piece(Piece.k, side)));
                        }
                    }

                }
            }
            Pop_bit(ref KingBB, From);

        }
        //void generate_moves()
        //{
        //    int source_square;
        //    int target_square;

        //    ulong bitboard;
        //    ulong attacks;

        //    for(int piece = (int)Piece.P; piece <= (int)Piece.k; piece++)
        //    {
        //        bitboard = bitboards[piece];

        //        if(side == (int)Side.White)
        //        {
        //            if(piece == (int)Piece.P)
        //            {
        //                while(bitboard != 0)
        //                {
        //                    source_square = get_ls1b(bitboard);


        //                    target_square = source_square - 8;

        //                    if(!(target_square < 0) && !Get_bit(occupancies[(int)Side.Both], target_square))
        //                    {
        //                        if(source_square >= (int)Square.a7 && source_square <= (int)Square.h7)
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "q");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "r");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "b");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "n");
        //                        }
        //                        else
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square));
        //                            if((source_square >= (int)Square.a2 && source_square <= (int)Square.h2) && !Get_bit(occupancies[(int)Side.Both], target_square - 8))
        //                            {
        //                                Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square - 8));
        //                            }
        //                        }
        //                    }

        //                    attacks = pawn_attacks[side, source_square] & occupancies[(int)Side.Black];

        //                    while(attacks != 0)
        //                    {

        //                        target_square = get_ls1b(attacks);
        //                        if (source_square >= (int)Square.a7 && source_square <= (int)Square.h7)
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "q");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "r");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "b");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "n");
        //                        }
        //                        else
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square));
        //                        }
        //                        Pop_bit(ref attacks, target_square);
        //                    }
        //                    if(enpassent != (int)Square.no_sq)
        //                    {
        //                        ulong enpassent_attacks = pawn_attacks[side, source_square] & (ulong)(1UL << enpassent);

        //                        if(enpassent_attacks != 0)
        //                        {
        //                            int target_enpassent = get_ls1b(enpassent_attacks);
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_enpassent));
        //                        }
        //                    }

        //                    Pop_bit(ref bitboard, source_square);

        //                }
        //            }
        //            else if(piece == (int)Piece.K)
        //            {

        //                if ((castle & WhiteKingCastle) != 0)
        //                {
        //                    if (!Get_bit(occupancies[(int)Side.Both], (int)Square.f1) && !Get_bit(occupancies[(int)Side.Both], (int)Square.g1))
        //                    {
        //                        if (!is_square_attacked((int)Square.e1, (int)Side.Black) && !is_square_attacked((int)Square.f1, (int)Side.Black) && !is_square_attacked((int)Square.g1, (int)Side.Black))
        //                        {
        //                            Console.WriteLine("castling e1g1");
        //                        }

        //                    }
        //                }

        //                if ((castle & WhiteQueenCastle) != 0)
        //                {
        //                    //Console.Write((castle & WhiteQueenCastle));
        //                    //Console.Write("adfs");
        //                    if (!Get_bit(occupancies[(int)Side.Both], (int)Square.d1) && !Get_bit(occupancies[(int)Side.Both], (int)Square.c1) && !Get_bit(occupancies[(int)Side.Both], (int)Square.b1))
        //                    {
        //                        //Console.Write("adfs");
        //                        if (!is_square_attacked((int)Square.e1, (int)Side.Black) && !is_square_attacked((int)Square.d1, (int)Side.Black) && !is_square_attacked((int)Square.c1, (int)Side.Black))
        //                        {
        //                            Console.WriteLine("castling e1c1");
        //                        }

        //                    }
        //                }
        //            }
        //        }
        //        else
        //        {
        //            if (piece == (int)Piece.p)
        //            {

        //                while (bitboard != 0)
        //                {
        //                    //Console.WriteLine("f");
        //                    source_square = get_ls1b(bitboard);


        //                    target_square = source_square + 8;

        //                    if (!(target_square > (int)Square.h1) && !Get_bit(occupancies[(int)Side.Both], target_square))
        //                    {
        //                        if (source_square >= (int)Square.a2 && source_square <= (int)Square.h2)
        //                        {
        //                            //Console.WriteLine("f");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "q");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "r");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "b");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "n");
        //                        }
        //                        else
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square));
        //                            if ((source_square >= (int)Square.a7 && source_square <= (int)Square.h7) && !Get_bit(occupancies[(int)Side.Both], target_square + 8))
        //                            {
        //                                Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square + 8));
        //                            }
        //                        }
        //                    }
        //                    attacks = pawn_attacks[side, source_square] & occupancies[(int)Side.White];

        //                    while (attacks != 0)
        //                    {

        //                        target_square = get_ls1b(attacks);
        //                        if (source_square >= (int)Square.a2 && source_square <= (int)Square.h2)
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "q");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "r");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "b");
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square) + "n");
        //                        }
        //                        else
        //                        {
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square));
        //                        }
        //                        Pop_bit(ref attacks, target_square);
        //                    }
        //                    if (enpassent != (int)Square.no_sq)
        //                    {
        //                        ulong enpassent_attacks = pawn_attacks[side, source_square] & (ulong)(1UL << enpassent);


        //                        if (enpassent_attacks != 0)
        //                        {
        //                            int target_enpassent = get_ls1b(enpassent_attacks);
        //                            Console.WriteLine("white pawn " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_enpassent));
        //                        }
        //                    }
        //                    Pop_bit(ref bitboard, source_square);

        //                }

        //            }
        //            else if (piece == (int)Piece.k)
        //            {

        //                if ((castle & BlackKingCastle) != 0)
        //                {
        //                    if (!Get_bit(occupancies[(int)Side.Both], (int)Square.f8) && !Get_bit(occupancies[(int)Side.Both], (int)Square.g8))
        //                    {
        //                        if (!is_square_attacked((int)Square.e8, (int)Side.White) && !is_square_attacked((int)Square.f8, (int)Side.White) && !is_square_attacked((int)Square.g8, (int)Side.White))
        //                        {
        //                            Console.WriteLine("castling e8g8");
        //                        }

        //                    }
        //                }

        //                if ((castle & BlackQueenCastle) != 0)
        //                {
        //                    //Console.Write((castle & WhiteQueenCastle));
        //                    //Console.Write("adfs");
        //                    if (!Get_bit(occupancies[(int)Side.Both], (int)Square.d8) && !Get_bit(occupancies[(int)Side.Both], (int)Square.c8) && !Get_bit(occupancies[(int)Side.Both], (int)Square.b8))
        //                    {
        //                        //Console.Write("adfs");
        //                        if (!is_square_attacked((int)Square.e8, (int)Side.White) && !is_square_attacked((int)Square.d8, (int)Side.White) && !is_square_attacked((int)Square.c8, (int)Side.White))
        //                        {
        //                            Console.WriteLine("castling e8c8");
        //                        }

        //                    }
        //                }
        //            }

        //        }
        //        if((side == (int)Side.White) ? piece == (int)Piece.N : piece == (int)Piece.n)
        //        {
        //            while(bitboard != 0)
        //            {
        //                source_square = get_ls1b(bitboard);

        //                attacks = Knight_attacks[source_square] & ((side == (int)Side.White) ? ~occupancies[(int)Side.White] : ~occupancies[(int)Side.Black]);

        //                while (attacks != 0)
        //                {
        //                    target_square = get_ls1b(attacks);

        //                    if (!Get_bit(((side == (int)Side.White) ? occupancies[(int)Side.Black] : occupancies[(int)Side.White]), target_square))
        //                    {
        //                        Console.WriteLine("piece quiet " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square));

        //                    }
        //                    else
        //                    {
        //                        Console.WriteLine("piece capture " + CoordinatesToChessNotation(source_square) + CoordinatesToChessNotation(target_square));
        //                    }


        //                    Pop_bit(ref attacks, target_square);
        //                }

        //                Pop_bit(ref bitboard, source_square);
        //            }
        //        }


        //    }
        //}
        bool is_square_attacked(int square, int side, Board board, ulong occupancy)
        {
            if ((side == Side.White) && ((pawn_attacks[Side.Black, square] & board.bitboards[Piece.P])) != 0) return true;
            if ((side == Side.Black) && ((pawn_attacks[Side.White, square] & board.bitboards[Piece.p])) != 0) return true;
            if ((get_bishop_attacks(square, occupancy) & ((side == Side.White) ? board.bitboards[Piece.B] : board.bitboards[Piece.b])) != 0) return true;
            if ((get_rook_attacks(square, occupancy) & ((side == Side.White) ? board.bitboards[Piece.R] : board.bitboards[Piece.r])) != 0) return true;
            if ((get_queen_attacks(square, occupancy) & ((side == Side.White) ? board.bitboards[Piece.Q] : board.bitboards[Piece.q])) != 0) return true;
            if ((Knight_attacks[square] & ((side == Side.White) ? board.bitboards[Piece.N] : board.bitboards[Piece.n])) != 0) return true;
            if ((King_attacks[square] & ((side == Side.White) ? board.bitboards[Piece.K] : board.bitboards[Piece.k])) != 0) return true;
            
            
            return false;
        }
        void print_attacked_squares(int side, Board board)
        {
            Console.Write("\n\n");
            for(int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;

                    if(file == 0)
                    {
                        Console.Write("   " + (8 - rank).ToString());
                    }
                    Console.Write(" "+(is_square_attacked(square, side, board, board.occupancies[Side.Both]) ? 1 : 0).ToString());
                }
                Console.Write("\n");

            }
            Console.Write("     a b c d e f g h\n\n");
        } 

        //ulong get_attacked_squares(int side, Board board, ulong occupancy)
        //{
        //    ulong attack_map = 0;
        //    ulong bb;

        //    //ulong occupancy = board.occupancies[Side.Both];

        //    if (side == Side.White)
        //    {
        //        bb = board.bitboards[Piece.K];

        //        attack_map |= King_attacks[get_ls1b(bb)];

        //        bb = board.bitboards[Piece.N];

        //        for(; bb != 0; )
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= Knight_attacks[loc];

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.B];

        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= get_bishop_attacks(loc, occupancy);

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.R];
        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= get_rook_attacks(loc, occupancy);

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.Q];
        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= get_queen_attacks(loc, occupancy);

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.P];
        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= pawn_attacks[side, loc];

        //            Pop_bit(ref bb, loc);
        //        }

        //    }
        //    else
        //    {
        //        bb = board.bitboards[Piece.k];

        //        attack_map |= King_attacks[get_ls1b(bb)];

        //        bb = board.bitboards[Piece.n];

        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= Knight_attacks[loc];

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.b];

        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= get_bishop_attacks(loc, occupancy);

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.r];
        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= get_rook_attacks(loc, occupancy);

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.q];
        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= get_queen_attacks(loc, occupancy);

        //            Pop_bit(ref bb, loc);
        //        }

        //        bb = board.bitboards[Piece.p];
        //        for (; bb != 0;)
        //        {
        //            int loc = get_ls1b(bb);

        //            attack_map |= pawn_attacks[side, loc];

        //            Pop_bit(ref bb, loc);
        //        }
        //    }

        //    return attack_map;
        //}
        ulong get_attacked_squares(int side, Board board, ulong occupancy)
        {
            ulong attack_map = 0;
            ulong bb;

            // Precompute piece bitboards for the given side
            ulong kingBB = (side == Side.White) ? board.bitboards[Piece.K] : board.bitboards[Piece.k];
            ulong knightBB = (side == Side.White) ? board.bitboards[Piece.N] : board.bitboards[Piece.n];
            ulong bishopBB = (side == Side.White) ? board.bitboards[Piece.B] : board.bitboards[Piece.b];
            ulong rookBB = (side == Side.White) ? board.bitboards[Piece.R] : board.bitboards[Piece.r];
            ulong queenBB = (side == Side.White) ? board.bitboards[Piece.Q] : board.bitboards[Piece.q];
            ulong pawnBB = (side == Side.White) ? board.bitboards[Piece.P] : board.bitboards[Piece.p];

            // Process King
            attack_map |= King_attacks[get_ls1b(kingBB)];

            // Process Knights
            for (bb = knightBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= Knight_attacks[loc];
                Pop_bit(ref bb, loc);
            }

            // Process Bishops
            for (bb = bishopBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= get_bishop_attacks(loc, occupancy);
                Pop_bit(ref bb, loc);
            }

            // Process Rooks
            for (bb = rookBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= get_rook_attacks(loc, occupancy);
                Pop_bit(ref bb, loc);
            }

            // Process Queens
            for (bb = queenBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= get_queen_attacks(loc, occupancy);
                Pop_bit(ref bb, loc);
            }

            // Process Pawns
            for (bb = pawnBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= pawn_attacks[side, loc];
                Pop_bit(ref bb, loc);
            }

            return attack_map;
        }
        public static string CoordinatesToChessNotation(int square)
        {
            int rawFile = square % 8;
            int rawRank = square == 0 ? 8 : 8 - square / 8;
            char File = (char)('a' + rawFile); // Convert column index to letter ('a' to 'h')
            int row = rawRank; // Row number (1 to 8)

            // Validate row
            if (row < 0 || row > 8)
            {
                throw new ArgumentException("Invalid chess square.");
            }

            return File.ToString() + row;
        }
        int getFile(int square)
        {
            return square % 8;
        }
        int getRank(int square)
        {
            return square == 0 ? 8 : 8 - square / 8;
        }

        void parse_fen(string fen, Board board)
        {
            //start_position
            //r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1 "
            //tricky_position
            //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 
            for(int i = 0; i < 64; i++)
            {
                board.mailbox[i] = -1;
            }
            //board.mailbox
            for (int i = 0; i < board.bitboards.Length; i++)
            {
                board.bitboards[i] = 0;
            }
            for (int i = 0; i < board.occupancies.Length; i++)
            {
                board.occupancies[i] = 0;
            }
            board.side = 0;
            board.enpassent = (int)Square.no_sq;
            //int index = 0;
            // Console.WriteLine(fen);
            int square = 0;
            int index = 0;
            for(int i = 0; i < fen.Length; i++)
            {
                char text = fen[i];
                //int file = square % 8;
                //int rank = square == 0 ? 0 : 8 - square / 8;
                if (text == ' ')
                {
                    index = i + 1;
                    break;
                }
                if (text == '/')
                {
                    //i++;
                    continue;

                }
                if (text >= '0' && text <= '9')
                {
                    //Console.WriteLine(square);
                    square += text - '0';
                    //Console.WriteLine(square);
                }

                //Console.WriteLine(i);
                if (text >= 'a' && text <= 'z' || text >= 'A' && text <= 'Z')
                {
                    int piece = char_pieces[text];
                    board.mailbox[square] = piece;
                    Set_bit(ref board.bitboards[piece], square);
                    square++;
                    //Console.WriteLine(piece);
                }

                //if (square >= 64) Console.WriteLine("bug");
                
            }
            if (fen[index] == 'w')
                board.side = (int)Side.White;
            else
                board.side = (int)Side.Black;

            index+= 2;

            for(int i = 0; i < 4; i++)
            {
                if (fen[index] == 'K') board.castle |= WhiteKingCastle;
                if (fen[index] == 'Q') board.castle |= WhiteQueenCastle;
                if (fen[index] == 'k') board.castle |= BlackKingCastle;
                if (fen[index] == 'q') board.castle |= BlackQueenCastle;
                if (fen[index] == ' ') break;
                index++;
            }
            index++;
            if (fen[index] != '-')
            {
                int file = fen[index] - 'a';
                int rank = 8 - (fen[index+1] - '0');

                board.enpassent = rank * 8 + file;

            }
            else
            {
                board.enpassent = (int)Square.no_sq;
            }
            for (int piece = (int)Piece.P; piece <= (int)Piece.K; piece++)
            {
                board.occupancies[(int)Side.White] |= board.bitboards[piece];
            }
            for (int piece = (int)Piece.p; piece <= (int)Piece.k; piece++)
            {
                board.occupancies[(int)Side.Black] |= board.bitboards[piece];
            }
            board.occupancies[(int)Side.Both] |= board.occupancies[(int)Side.Black];
            board.occupancies[(int)Side.Both] |= board.occupancies[(int)Side.White];
        }
        static void PrintBitboard(ulong bitboard)
        {
            Console.Write("\n");
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    Console.Write((bitboard & (1UL << square)) != 0 ? "1 " : "0 ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        void PrintBoards(Board board)
        {
            Console.Write("\n");
            for(int rank = 0; rank < 8; rank++)
            {
                for(int file = 0; file < 8;file++)
                {
                    int square = rank * 8 + file;
                    if (file == 0)
                    {
                        Console.Write(" "+ (8 - rank) + " ");
                    }
                    
                    int piece = -1;

                    for(int bb_piece = (int)Piece.P; bb_piece <= (int)Piece.k; bb_piece++)
                    {
                        if (Get_bit(board.bitboards[bb_piece], square))
                        {
                            piece = bb_piece;
                        }
                    }

                    Console.Write(" "+ ((piece == -1) ? '.' : ascii_pieces[piece]));
                }
                Console.Write("\n");
            }
            
            Console.Write("\n    a b c d e f g h");
            Console.Write("\n    Side :     " + (board.side == 0 ?"w" : "b"));
            Console.Write("\n    Enpassent :     " + (board.enpassent != (int)Square.no_sq ? CoordinatesToChessNotation(board.enpassent) : "no"));
            Console.Write("\n    Castling :     " + ((((ulong)board.castle & WhiteKingCastle) != 0) ? 'K':'-') + ((((ulong)board.castle & WhiteQueenCastle) != 0) ? 'Q' : '-') + ((((ulong)board.castle & BlackKingCastle) != 0) ? 'k' : '-')+ ((((ulong)board.castle & BlackQueenCastle) != 0) ? 'q' : '-'));
            Console.Write("\n");
        }

        void print_mailbox(int[] mailbox)
        {
            Console.Write("\n MAILBOX \n");
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    if (file == 0)
                    {
                        Console.Write(" " + (8 - rank) + " ");
                    }
                    //int piece = 0;
                    if (mailbox[square] != -1) //
                    {

                        Console.Write(" " + ascii_pieces[mailbox[square]]);
                    }
                    else
                    {
                        Console.Write(" .");
                    }
                }
                Console.Write("\n");
            }

        }

        static void print_bitboard(ulong board)
        {
            for (int rank = 0; rank < 8; rank++)
            {


                for (int file = 0; file < 8; file++)
                {

                    int square = rank * 8 + file;
                    Console.Write(" " + ((Get_bit(board, square) ? 1 : 0) + " "));

                }
                Console.Write("\n");
            }
            Console.WriteLine(board);


        }
        uint get_random_number()
        {
            uint number = state;
            number ^= number << 13;
            number ^= number >> 17;
            number ^= number << 5;

            state = number;
            return number;
        }
        ulong find_magic(int square, int relevant_bits, int bishop)
        {
            ulong[] occupancies = new ulong[4096];
            ulong[] attacks = new ulong[4096];
            ulong[] used_attacks = new ulong[4096];

            ulong attack_mask = bishop == 1 ? MaskBishopAttack(square) : MaskRookAttack(square);

            int occupancy_indicies = 1 << relevant_bits;

            for (int i = 0; i < occupancy_indicies; i++)
            {
                occupancies[i] = set_occupancy(i, relevant_bits, attack_mask);
                attacks[i] = bishop == 1 ? CalculateBishopAttack(square, occupancies[i]) : CalculateRookAttack(square, occupancies[i]);
            }

            //test magic number
            for (int random_count = 0; random_count < 10000000; random_count++)
            {
                ulong magic_number = generate_magic_num();
                if (count_bits((attack_mask * magic_number) & 0xFF00000000000000) < 6) continue;

                bool fail = false;

                // Reset used_attacks array
                Array.Clear(used_attacks, 0, used_attacks.Length);

                for (int index = 0; index < occupancy_indicies; index++)
                {
                    int magic_index = (int)((occupancies[index] * magic_number) >> (64 - relevant_bits));

                    if (used_attacks[magic_index] == 0)
                    {
                        used_attacks[magic_index] = attacks[index];
                    }
                    else if (used_attacks[magic_index] != attacks[index])
                    {
                        fail = true;
                        break;
                    }
                }

                if (!fail)
                {
                    return magic_number;
                }
            }

            Console.WriteLine("magic number fails");
            return 0;
        }

        void init_magic_numbers()
        {
            for (int square = 0; square < 64; square++)
            {
                ulong magic_number = find_magic(square, rook_relevant_bits[square], 0);
                //Console.WriteLine($"Magic number: 0x{magic_number:X11}ULL");
                rook_magic_numbers[square] = magic_number;
            }
            Console.Write("\n");
            for (int square = 0; square < 64; square++)
            {
                ulong magic_number = find_magic(square, bishop_relevant_bits[square], 1);
                //Console.WriteLine($"Magic number: 0x{magic_number:X11}ULL");
                bishop_magic_numbers[square] = magic_number;
            }
        }

        void init_sliders_attacks(int bishop)
        {
            for (int square = 0; square < 64; square++)
            {
                bishop_masks[square] = MaskBishopAttack(square);
                rook_masks[square] = MaskRookAttack(square);

                ulong attack_mask = bishop != 0 ? bishop_masks[square] : rook_masks[square];
                int relevant_bits_count = count_bits(attack_mask);
                int occupancy_indicies = (1 << relevant_bits_count);

                for (int index = 0; index < occupancy_indicies; index++)
                {
                    if (bishop != 0)
                    {
                        ulong occupancy = set_occupancy(index, relevant_bits_count, attack_mask);

                        int magic_index = (int)((occupancy * bishop_magic_numbers[square]) >> (64 - bishop_relevant_bits[square]));
                        // 인덱스 범위를 확인하여 오류 방지
                        if (magic_index < 512)
                        {
                            bishop_attacks[square, magic_index] = CalculateBishopAttack(square, occupancy);
                        }
                        else
                        {
                            Console.WriteLine($"bishop magic_index out of range: {magic_index} for square: {square}");
                        }
                    }
                    else
                    {
                        ulong occupancy = set_occupancy(index, relevant_bits_count, attack_mask);

                        int magic_index = (int)((occupancy * rook_magic_numbers[square]) >> (64 - rook_relevant_bits[square]));
                        // 인덱스 범위를 확인하여 오류 방지
                        if (magic_index < 4096)
                        {
                            rook_attacks[square, magic_index] = CalculateRookAttack(square, occupancy);
                        }
                        else
                        {
                            Console.WriteLine($"rook magic_index out of range: {magic_index} for square: {square}");
                        }
                    }
                }
            }
        }

        ulong get_bishop_attacks(int square, ulong occupancy)
        {
            occupancy &= bishop_masks[square];
            occupancy *= bishop_magic_numbers[square];
            occupancy >>= 64 - bishop_relevant_bits[square];

            return bishop_attacks[square,occupancy];
        }
        ulong get_rook_attacks(int square, ulong occupancy)
        {
            occupancy &= rook_masks[square];
            occupancy *= rook_magic_numbers[square];
            occupancy >>= 64 - rook_relevant_bits[square];

            return rook_attacks[square, occupancy];
        }
        ulong get_queen_attacks(int square, ulong occupancy)
        {
            ulong queen_attacks;
            ulong bishop_occupancies = occupancy;
            ulong rook_occupancies = occupancy;

            rook_occupancies &= rook_masks[square];
            rook_occupancies *= rook_magic_numbers[square];
            rook_occupancies >>= 64 - rook_relevant_bits[square];
            queen_attacks = rook_attacks[square, rook_occupancies];

            bishop_occupancies &= bishop_masks[square];
            bishop_occupancies *= bishop_magic_numbers[square];
            bishop_occupancies >>= 64 - bishop_relevant_bits[square];
            queen_attacks |= bishop_attacks[square, bishop_occupancies];

            return queen_attacks;
        }
        ulong get_ulong_rand()
        {
            ulong n1, n2, n3, n4;
            n1 = (ulong)(get_random_number() & 0xFFFF);
            n2 = (ulong)(get_random_number() & 0xFFFF);
            n3 = (ulong)(get_random_number() & 0xFFFF);
            n4 = (ulong)(get_random_number() & 0xFFFF);

            return n1 | (n2 << 16) | (n3 << 32) | (n4 << 48);
        }
        ulong generate_magic_num()
        {
            return get_ulong_rand() & get_ulong_rand() & get_ulong_rand();
        }

        static ulong CalculatePawnAttack(int square, int side)
        {
            ulong attacks = 0UL;
            ulong bitboard = 0UL;
            Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            if (side == 0)//white
            {
                if (((bitboard >> 7) & NotAFile) != 0)
                    attacks |= (bitboard >> 7);
                if (((bitboard >> 9) & NotHFile) != 0)
                    attacks |= (bitboard >> 9);
            }
            else //black
            {
                if (((bitboard << 7) & NotHFile) != 0)
                    attacks |= (bitboard << 7);
                if (((bitboard << 9) & NotAFile) != 0)
                    attacks |= (bitboard << 9);
            }

            return attacks;
        }

        static ulong CalculateKnightAttack(int square)
        {
            ulong attacks = 0UL;
            ulong bitboard = 0UL;
            Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6

            if (((bitboard >> 17) & NotHFile) != 0)
                attacks |= (bitboard >> 17);
            if (((bitboard >> 15) & NotAFile) != 0)
                attacks |= (bitboard >> 15);
            if (((bitboard >> 10) & NotHGFile) != 0)
                attacks |= (bitboard >> 10);
            if (((bitboard >> 6) & NotABFile) != 0)
                attacks |= (bitboard >> 6);

            if (((bitboard << 17) & NotAFile) != 0)
                attacks |= (bitboard << 17);
            if (((bitboard << 15) & NotHFile) != 0)
                attacks |= (bitboard << 15);
            if (((bitboard << 10) & NotABFile) != 0)
                attacks |= (bitboard << 10);
            if (((bitboard << 6) & NotHGFile) != 0)
                attacks |= (bitboard << 6);
            return attacks;
        }
        static ulong CalculateKingAttack(int square)
        {
            ulong attacks = 0UL;
            ulong bitboard = 0UL;
            Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6

            if (((bitboard >> 8)) != 0)
                attacks |= (bitboard >> 8);
            if (((bitboard >> 9) & NotHFile) != 0)
                attacks |= (bitboard >> 9);
            if (((bitboard >> 7) & NotAFile) != 0)
                attacks |= (bitboard >> 7);
            if (((bitboard >> 1) & NotHFile) != 0)
                attacks |= (bitboard >> 1);

            if (((bitboard << 8)) != 0)
                attacks |= (bitboard << 8);
            if (((bitboard << 9) & NotAFile) != 0)
                attacks |= (bitboard << 9);
            if (((bitboard << 7) & NotHFile) != 0)
                attacks |= (bitboard << 7);
            if (((bitboard << 1) & NotAFile) != 0)
                attacks |= (bitboard << 1);
            return attacks;
        }

        static ulong CalculateBishopAttack(int square, ulong block)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1, f = tf + 1; r <= 7 && f <= 7; r++, f++)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr - 1, f = tf + 1; r >= 0 && f <= 7; r--, f++)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr + 1, f = tf - 1; r <= 7 && f >= 0; r++, f--)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr - 1, f = tf - 1; r >= 0 && f >= 0; r--, f--)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }

        static ulong MaskBishopAttack(int square)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1, f = tf + 1; r <= 6 && f <= 6; r++, f++)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            for (r = tr - 1, f = tf + 1; r >= 1 && f <= 6; r--, f++)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            for (r = tr + 1, f = tf - 1; r <= 6 && f >= 1; r++, f--)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            for (r = tr - 1, f = tf - 1; r >= 1 && f >= 1; r--, f--)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }

        static ulong CalculateRookAttack(int square, ulong block)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1; r <= 7; r++)
            {
                attacks |= (1UL << (r * 8 + tf));
                if ((1UL << (r * 8 + tf) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr - 1; r >= 0; r--)
            {
                attacks |= (1UL << (r * 8 + tf));
                if ((1UL << (r * 8 + tf) & block) != 0)
                {
                    break;
                }
            }
            for (f = tf + 1; f <= 7; f++)
            {
                attacks |= (1UL << (tr * 8 + f));
                if ((1UL << (tr * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (f = tf - 1; f >= 0; f--)
            {
                attacks |= (1UL << (tr * 8 + f));
                if ((1UL << (tr * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }
        static ulong MaskRookAttack(int square)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1; r <= 6; r++)
            {
                attacks |= (1UL << (r * 8 + tf));

            }
            for (r = tr - 1; r >= 1; r--)
            {
                attacks |= (1UL << (r * 8 + tf));

            }
            for (f = tf + 1; f <= 6; f++)
            {
                attacks |= (1UL << (tr * 8 + f));

            }
            for (f = tf - 1; f >= 1; f--)
            {
                attacks |= (1UL << (tr * 8 + f));

            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }
        static ulong set_occupancy(int index, int bits_in_mask, ulong attack_mask)
        {
            ulong occupancy = 0;

            for (int count = 0; count < bits_in_mask; count++)
            {
                int square = get_ls1b(attack_mask);

                Pop_bit(ref attack_mask, square);
                if ((index & (1 << count)) != 0)
                {
                    occupancy |= (1UL << square);
                }
            }
            return occupancy;
        }
        static bool Get_bit(ulong bit, int a)
        {
            return ((bit & (1UL << a)) != 0);
        }
        static void Set_bit(ref ulong bit, int a)
        {
            bit |= 1UL << a;
        }
        static void Pop_bit(ref ulong bit, int a)
        {
            if (Get_bit(bit, a))
            {
                bit ^= (1UL << a);
            }

            //bit |= 1UL << a;
        }
        static int count_bits(ulong bitboard)
        {
            int count = 0;
            while (bitboard > 0)
            {
                count++;
                bitboard &= bitboard - 1;
            }
            return count;
        }
        static int get_ls1b(ulong bitboard)
        {
            if (bitboard > 0)
            {   

                return count_bits((bitboard & 0 - bitboard) - 1);
            }
            else
            {
                return -1;
            }
        }
        void InitializeLeaper()
        {
            for (int i = 0; i < 64; i++)
            {
                pawn_attacks[0, i] = CalculatePawnAttack(i, 0);
                pawn_attacks[1, i] = CalculatePawnAttack(i, 1);

                Knight_attacks[i] = CalculateKnightAttack(i);
                King_attacks[i] = CalculateKingAttack(i);


            }
        }
    }
}






