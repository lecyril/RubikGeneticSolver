using System;
using System.Linq;
using System.Collections.Generic;

namespace Rubik
{
    internal static class ExternalFcts
    {
        static Random _rng = new Random();

        public static void InitRandomSeed()
        {
            // seed based on current time (different each run)
            unchecked
            {
                _rng = new Random((int)(DateTime.UtcNow.Ticks & 0x7FFFFFFF));
            }
        }

        public static double RandomDouble() => _rng.NextDouble();

        public static int RandomInt(int exclusive) => _rng.Next(exclusive);

        /// <summary>
        /// Initializes a cube in the solved state and returns an array representing the faces.Init cube: returns array[54] with blocks of 9 having same value 0..5
        /// </summary>
        /// <remarks>Each value from 0 to 5 represents a distinct face of the cube. The returned array can
        /// be used to represent the state of a standard 3󫢫 cube, with each face initialized to a uniform color or
        /// identifier.</remarks>
        /// <returns>An array of 54 integers where each group of 9 consecutive elements corresponds to a face of the cube, and
        /// all elements in a group have the same value from 0 to 5.</returns>
        public static int[] InitCube()
        {
            var res = new int[54];
            for (int face = 0; face < 6; face++)
                for (int p = 0; p < 9; p++)
                    res[face * 9 + p] = face;
            return res;
        }

        /// <summary>
        /// Converts a sequence of move numbers into an array of strings representing each move in standard notation.
        /// </summary>
        /// <remarks>The face characters used are 'F', 'U', 'R', 'B', 'D', and 'L'. The modifier is a
        /// space for a single turn, '2' for a double turn, and '\'' for a counterclockwise turn. The output array
        /// length is three times the number of moves in the input sequence.</remarks>
        /// <param name="sequence">An array of integers where each element represents a move. Each move should correspond to a face and
        /// modifier according to the notation scheme.</param>
        /// <returns>An array of strings containing the notation for each move. Each move is represented by three consecutive
        /// string entries: the face character, the move modifier (a space, '2', or '\''), and a space. Returns an empty
        /// array if <paramref name="sequence"/> is null.</returns>
        // Each move => three entries: face char, modifier (' ', '2' or '\''), ' '
        public static string[] ToNotation(int[] sequence)
        {
            if (sequence == null) return Array.Empty<string>();
            var outArr = new string[sequence.Length * 3];
            for (int a = 0; a < sequence.Length; a++)
            {
                int move = sequence[a];
                int face = move % 6;
                string faceChar = face switch
                {
                    0 => "F",
                    1 => "U",
                    2 => "R",
                    3 => "B",
                    4 => "D",
                    5 => "L",
                    _ => "?"
                };
                outArr[3 * a + 0] = faceChar;
                if (move > 11) outArr[3 * a + 1] = "'";
                else if (move > 5) outArr[3 * a + 1] = "2";
                else outArr[3 * a + 1] = " ";
                outArr[3 * a + 2] = " ";
            }
            return outArr;
        }

        /// <summary>
        /// Parses a move notation string and returns its corresponding numeric code.ToNumbers: parses string like "F", "F2", "F'" (case-insensitive)
        /// </summary>
        /// <remarks>The method supports standard face notations ('F', 'U', 'R', 'B', 'D', 'L') with
        /// optional modifiers: '2' for a double turn and '\'' for a counterclockwise turn. For example, "F" returns 0,
        /// "F2" returns 6, and "F'" returns 12.</remarks>
        /// <param name="s">The move notation to parse. The string can be in the form of a single face letter (e.g., "F"), optionally
        /// followed by a modifier such as '2' or a prime symbol ('). The comparison is case-insensitive. Leading and
        /// trailing whitespace are ignored.</param>
        /// <returns>An integer representing the numeric code for the specified move notation. Returns 0 if the input is null,
        /// empty, or unrecognized.</returns>
        public static int ToNumbers(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0;
            s = s.Trim().ToUpperInvariant();
            char c0 = s.Length > 0 ? s[0] : '\0';
            int val = c0 switch
            {
                'F' => 0,
                'U' => 1,
                'R' => 2,
                'B' => 3,
                'D' => 4,
                'L' => 5,
                _ => 0
            };
            if (s.Length > 1)
            {
                if (s[1] == '2') val += 6;
                else if (s[1] == '\'') val += 12;
            }
            return val;
        }

        /// <summary>
        /// Applies a sequence of moves to the specified cube and returns the resulting cube state.
        /// </summary>
        /// <param name="cube">An array representing the initial state of the cube. Cannot be null.</param>
        /// <param name="moveSequence">An array of zero-based move indices to apply to the cube. If null or empty, the original cube state is
        /// returned.</param>
        /// <returns>An array representing the cube state after all moves in the sequence have been applied. If no moves are
        /// specified, a copy of the original cube is returned.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cube"/> is null.</exception>
        public static int[] DoSequence(int[] cube, int[] moveSequence)
        {
            if (cube == null) throw new ArgumentNullException(nameof(cube));
            var cur = (int[])cube.Clone();
            if (moveSequence == null || moveSequence.Length == 0) return cur;
            foreach (var m in moveSequence)
            {
                cur = DoMove(cur, m);
            }
            return cur;
        }

        
        /// <summary>
        /// Applies a single move to the given cube state, including repetitions for double, triple, or full turns, and
        /// returns the resulting state.
        /// </summary>
        /// <remarks>The method does not modify the input array; instead, it returns a new array with the
        /// updated state. The interpretation of the move parameter depends on the cube's move encoding, where higher
        /// values indicate multiple turns of the same face.</remarks>
        /// <param name="cubeState">An array representing the current state of the cube. The array must not be null and should follow the
        /// expected cube state format.</param>
        /// <param name="move">An integer specifying the move to apply. The value determines the face to turn and the number of repetitions
        /// (e.g., single, double, triple, or full turn).</param>
        /// <returns>An array representing the new state of the cube after the specified move has been applied.</returns>
        public static int[] DoMove(int[] cubeState, int move)
        {
            var res = (int[])cubeState.Clone();
            int repetition = 0;
            if (move > 5) repetition = 1;
            if (move > 11) repetition = 2;
            if (move > 17) repetition = 3; // full turn (kept for fidelity)

            for (int rep = 0; rep <= repetition; rep++)
            {
                var prev = (int[])res.Clone();
                // rotate face
                res = TurnFace(res, move % 6);

                // rotate ring depending on face
                switch (move % 6)
                {
                    case 0:
                        res[15] = prev[53];
                        res[16] = prev[50];
                        res[17] = prev[47];
                        res[18] = prev[15];
                        res[21] = prev[16];
                        res[24] = prev[17];
                        res[36] = prev[24];
                        res[37] = prev[21];
                        res[38] = prev[18];
                        res[47] = prev[36];
                        res[50] = prev[37];
                        res[53] = prev[38];
                        break;
                    case 1:
                        res[0] = prev[18];
                        res[1] = prev[19];
                        res[2] = prev[20];
                        res[18] = prev[27];
                        res[19] = prev[28];
                        res[20] = prev[29];
                        res[27] = prev[45];
                        res[28] = prev[46];
                        res[29] = prev[47];
                        res[45] = prev[0];
                        res[46] = prev[1];
                        res[47] = prev[2];
                        break;
                    case 2:
                        res[2] = prev[38];
                        res[5] = prev[41];
                        res[8] = prev[44];
                        res[11] = prev[2];
                        res[14] = prev[5];
                        res[17] = prev[8];
                        res[27] = prev[17];
                        res[30] = prev[14];
                        res[33] = prev[11];
                        res[38] = prev[33];
                        res[41] = prev[30];
                        res[44] = prev[27];
                        break;
                    case 3:
                        res[9] = prev[20];
                        res[10] = prev[23];
                        res[11] = prev[26];
                        res[20] = prev[44];
                        res[23] = prev[43];
                        res[26] = prev[42];
                        res[42] = prev[45];
                        res[43] = prev[48];
                        res[44] = prev[51];
                        res[45] = prev[11];
                        res[48] = prev[10];
                        res[51] = prev[9];
                        break;
                    case 4:
                        res[6] = prev[51];
                        res[7] = prev[52];
                        res[8] = prev[53];
                        res[24] = prev[6];
                        res[25] = prev[7];
                        res[26] = prev[8];
                        res[33] = prev[24];
                        res[34] = prev[25];
                        res[35] = prev[26];
                        res[51] = prev[33];
                        res[52] = prev[34];
                        res[53] = prev[35];
                        break;
                    case 5:
                        res[0] = prev[9];
                        res[3] = prev[12];
                        res[6] = prev[15];
                        res[9] = prev[35];
                        res[12] = prev[32];
                        res[15] = prev[29];
                        res[29] = prev[42];
                        res[32] = prev[39];
                        res[35] = prev[36];
                        res[36] = prev[0];
                        res[39] = prev[3];
                        res[42] = prev[6];
                        break;
                    default:
                        // move error - keep res as previous
                        break;
                }
            }

            return res;
        }

        /// <summary>
        /// Returns a new cube state with the specified face rotated 90 degrees clockwise.
        /// </summary>
        /// <remarks>The method assumes a standard 3x3 cube representation, with each face occupying 9
        /// consecutive elements in the array. Only the specified face is rotated; the rest of the cube remains
        /// unchanged.</remarks>
        /// <param name="cube">An array representing the current state of the cube. The array must contain at least 54 elements, where each
        /// group of 9 elements corresponds to a face of the cube.</param>
        /// <param name="face">The zero-based index of the face to rotate. Must be in the range 0 to 5, where each value corresponds to a
        /// different face of the cube.</param>
        /// <returns>A new array representing the cube state after rotating the specified face clockwise. The original array is
        /// not modified.</returns>
        static int[] TurnFace(int[] cube, int face)
        {
            var res = (int[])cube.Clone();
            int offset = face * 9;
            // mapping as in Fortran (mod used for safety)
            res[offset + 0] = cube[offset + 6];
            res[offset + 1] = cube[offset + 3];
            res[offset + 2] = cube[offset + 0];
            res[offset + 3] = cube[offset + 7];
            res[offset + 5] = cube[offset + 1];
            res[offset + 6] = cube[offset + 8];
            res[offset + 7] = cube[offset + 5];
            res[offset + 8] = cube[offset + 2];
            return res;
        }

        /// <summary>
        /// Compares two cube state arrays and returns a score indicating the degree of similarity and specific
        /// conditions met between them.
        /// </summary>
        /// <remarks>Both arrays must have a length of 54, corresponding to the standard number of
        /// stickers on a 3x3 Rubik's Cube. The method does not validate array lengths and may throw an exception if the
        /// arrays are not of the expected size.</remarks>
        /// <param name="cube1">An array of 54 integers representing the first cube's state. Each element corresponds to a sticker position
        /// on the cube.</param>
        /// <param name="cube2">An array of 54 integers representing the second cube's state. Each element corresponds to a sticker position
        /// on the cube.</param>
        /// <returns>An integer score representing the number of positions where the cubes are identical, plus additional points
        /// for positions in which the second cube's value exceeds 5 and the first cube's value is less than 6.</returns>
        public static int Compare(int[] cube1, int[] cube2)
        {
            int cmp = 0;
            for (int m = 0; m < 54; m++)
            {
                if (cube1[m] == cube2[m]) cmp++;
                if (cube2[m] > 5)
                {
                    if (cube1[m] < 6) cmp++;
                }
            }
            return cmp;
        }

        /// <summary>
        /// Calculates the entropy value for a given cube state, representing the number of specific matching pairs of
        /// stickers.
        /// </summary>
        /// <remarks>The entropy value can be used as a heuristic to estimate how close the cube is to a
        /// solved or partially solved state. A higher entropy indicates more matching pairs according to predefined
        /// relationships between stickers.</remarks>
        /// <param name="cube">An array of integers representing the current state of the cube. Each element corresponds to a sticker on
        /// the cube, where the value indicates the sticker's color or identity. The array must have 54
        /// elements.</param>
        /// <returns>The number of matching corner-edge and edge-center pairs found in the provided cube state.</returns>
        public static int Entropy(int[] cube)
        {
            int ent = 0;
            // Corner-edge pairs (kept faithful to Fortran)
            if (cube[0] == cube[1] && cube[15] == cube[16]) ent++;
            if (cube[0] == cube[3] && cube[47] == cube[50]) ent++;
            if (cube[47] == cube[46] && cube[15] == cube[12]) ent++;
            if (cube[2] == cube[5] && cube[18] == cube[21]) ent++;
            if (cube[2] == cube[1] && cube[17] == cube[16]) ent++;
            if (cube[17] == cube[14] && cube[18] == cube[19]) ent++;
            if (cube[8] == cube[5] && cube[24] == cube[21]) ent++;
            if (cube[8] == cube[7] && cube[38] == cube[37]) ent++;
            if (cube[38] == cube[41] && cube[24] == cube[25]) ent++;
            if (cube[6] == cube[3] && cube[53] == cube[50]) ent++;
            if (cube[6] == cube[7] && cube[36] == cube[37]) ent++;
            if (cube[36] == cube[39] && cube[53] == cube[52]) ent++;
            if (cube[29] == cube[32] && cube[45] == cube[48]) ent++;
            if (cube[29] == cube[28] && cube[9] == cube[10]) ent++;
            if (cube[9] == cube[12] && cube[45] == cube[46]) ent++;
            if (cube[27] == cube[28] && cube[11] == cube[10]) ent++;
            if (cube[27] == cube[30] && cube[20] == cube[23]) ent++;
            if (cube[11] == cube[14] && cube[20] == cube[19]) ent++;
            if (cube[33] == cube[34] && cube[44] == cube[43]) ent++;
            if (cube[33] == cube[30] && cube[26] == cube[23]) ent++;
            if (cube[44] == cube[41] && cube[26] == cube[25]) ent++;
            if (cube[35] == cube[34] && cube[42] == cube[43]) ent++;
            if (cube[35] == cube[32] && cube[51] == cube[48]) ent++;
            if (cube[42] == cube[39] && cube[51] == cube[52]) ent++;

            // Edges-centers pairs
            if (cube[4] == cube[1]) ent++;
            if (cube[4] == cube[3]) ent++;
            if (cube[4] == cube[5]) ent++;
            if (cube[4] == cube[7]) ent++;
            if (cube[13] == cube[10]) ent++;
            if (cube[13] == cube[12]) ent++;
            if (cube[13] == cube[14]) ent++;
            if (cube[13] == cube[16]) ent++;
            if (cube[22] == cube[19]) ent++;
            if (cube[22] == cube[21]) ent++;
            if (cube[22] == cube[23]) ent++;
            if (cube[22] == cube[25]) ent++;
            if (cube[31] == cube[28]) ent++;
            if (cube[31] == cube[30]) ent++;
            if (cube[31] == cube[32]) ent++;
            if (cube[31] == cube[34]) ent++;
            if (cube[40] == cube[37]) ent++;
            if (cube[40] == cube[39]) ent++;
            if (cube[40] == cube[41]) ent++;
            if (cube[40] == cube[43]) ent++;
            if (cube[49] == cube[46]) ent++;
            if (cube[49] == cube[48]) ent++;
            if (cube[49] == cube[50]) ent++;
            if (cube[49] == cube[52]) ent++;

            return ent;
        }

        /// <summary>
        /// Evaluates how closely the provided cube state matches the configuration of a 2x2x3 cuboid puzzle.
        /// </summary>
        /// <remarks>This method checks specific positions within the cube array to determine alignment
        /// with a 2x2x3 cuboid structure. It does not validate the overall solvability or legality of the cube
        /// state.</remarks>
        /// <param name="cube">An array of integers representing the current state of the cube. Each element corresponds to a sticker or
        /// position on the cube. The array must be of 54 length.</param>
        /// <returns>The number of conditions satisfied that indicate the cube matches a 2x2x3 configuration. Higher values
        /// indicate a closer match. 16 indicates a solved 2x2x3 state.</returns>
        public static int Is2x2x3(int[] cube)
        {
            int ok = 0;
            if (cube[33] == cube[34] && cube[44] == cube[43]) ok++;
            if (cube[33] == cube[30] && cube[26] == cube[23]) ok++;
            if (cube[44] == cube[41] && cube[26] == cube[25]) ok++;

            if (cube[35] == cube[34] && cube[42] == cube[43]) ok++;
            if (cube[35] == cube[32] && cube[51] == cube[48]) ok++;
            if (cube[42] == cube[39] && cube[51] == cube[52]) ok++;

            // edges-centers
            if (cube[22] == cube[23]) ok++;
            if (cube[22] == cube[25]) ok++;
            if (cube[31] == cube[30]) ok++;
            if (cube[31] == cube[32]) ok++;
            if (cube[31] == cube[34]) ok++;
            if (cube[40] == cube[39]) ok++;
            if (cube[40] == cube[41]) ok++;
            if (cube[40] == cube[43]) ok++;
            if (cube[49] == cube[48]) ok++;
            if (cube[49] == cube[52]) ok++;

            return ok;
        }

        /// <summary>
        /// Determines whether the given cube state is a member of the 2-generator subgroup based on its edge and corner
        /// configuration.
        /// </summary>
        /// <remarks>This method is typically used in Rubik's Cube solvers or analyzers to check if a cube
        /// state can be solved using only two face turns. </remarks>
        /// <param name="cube">An array of integers representing the cube's sticker colors or positions. The array must be of length 54.</param>
        /// <returns>1 if the cube state belongs to the 2-generator subgroup; otherwise, 0.</returns>
        public static int Is2gen(int[] cube)
        {
            int is2gen = 0;
            int edges_ori = 0;

            if (cube[1] == cube[4] || cube[16] == cube[13]) edges_ori++;
            if (cube[3] == cube[4] || cube[50] == cube[13]) edges_ori++;
            if (cube[5] == cube[4] || cube[21] == cube[13]) edges_ori++;
            if (cube[7] == cube[4] || cube[37] == cube[13]) edges_ori++;
            if (cube[28] == cube[4] || cube[10] == cube[13]) edges_ori++;
            if (cube[46] == cube[4] || cube[13] == cube[13]) edges_ori++;
            if (cube[19] == cube[4] || cube[14] == cube[13]) edges_ori++;

            if (edges_ori < 7) return 0;

            // corners
            int[] corner_temp = new int[6];
            int[] corners_pos = new int[6];

            // helper: 2**cube[i] -> 1 << cube[i]
            int BIT(int idx) => 1 << cube[idx];

            corner_temp[0] = BIT(6) + BIT(36) + BIT(53); // FDL
            corner_temp[1] = BIT(8) + BIT(38) + BIT(24); // FDR
            corner_temp[2] = BIT(0) + BIT(15) + BIT(47); // FUL
            corner_temp[3] = BIT(2) + BIT(17) + BIT(18); // FUR
            corner_temp[4] = BIT(29) + BIT(9) + BIT(45); // BUL
            corner_temp[5] = BIT(27) + BIT(11) + BIT(20); // BUR

            for (int co = 0; co < 6; co++)
            {
                int v = corner_temp[co];
                if (v == (BIT(4) + BIT(40) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(4) + BIT(40) + BIT(22))) corners_pos[co] = 1;
                else if (v == (BIT(4) + BIT(13) + BIT(49))) corners_pos[co] = 2;
                else if (v == (BIT(4) + BIT(13) + BIT(22))) corners_pos[co] = 2;
                else if (v == (BIT(31) + BIT(13) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(31) + BIT(13) + BIT(22))) corners_pos[co] = 1;
                else corners_pos[co] = 9; // unknown sentinel
            }

            int coVal = corners_pos[0] * 100000 + corners_pos[1] * 10000 + corners_pos[2] * 1000 + corners_pos[3] * 100 + corners_pos[4] * 10 + corners_pos[5];
            // list from Fortran
            int[] allowed = new int[] { 12201, 1221, 11022, 12120, 10212, 102210, 110220, 100122, 102021, 101202, 210021, 221001, 211200, 210102, 212010, 21102, 2112, 22011, 21210, 20121, 120012, 112002, 122100, 120201, 121020, 201120, 220110, 200211, 201012, 202101 };
            if (allowed.Contains(coVal)) is2gen = 1;
            else return 0;

            // second test: different mapping
            for (int co = 0; co < 6; co++)
            {
                int v = corner_temp[co];
                if (v == (BIT(4) + BIT(40) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(4) + BIT(40) + BIT(22))) corners_pos[co] = 1;
                else if (v == (BIT(4) + BIT(13) + BIT(49))) corners_pos[co] = 0;
                else if (v == (BIT(4) + BIT(13) + BIT(22))) corners_pos[co] = 2;
                else if (v == (BIT(31) + BIT(13) + BIT(49))) corners_pos[co] = 1;
                else if (v == (BIT(31) + BIT(13) + BIT(22))) corners_pos[co] = 2;
                else corners_pos[co] = 9;
            }

            coVal = corners_pos[0] * 100000 + corners_pos[1] * 10000 + corners_pos[2] * 1000 + corners_pos[3] * 100 + corners_pos[4] * 10 + corners_pos[5];
            if (allowed.Contains(coVal)) is2gen = 1;
            else is2gen = 0;

            return is2gen;
        }

        // FObj: returns int[2] {10*nmax - x, x}
        /// <summary>
        /// Evaluates a sequence of moves applied to a cube and returns an array containing a computed score and the
        /// position of the best move according to the specified evaluation function.
        /// </summary>
        /// <remarks>The evaluation function used is determined by the value of typeFunc. The method
        /// applies each move in movesSequence to a copy of the initial cube state, evaluates the result, and tracks the
        /// move that yields the highest evaluation. The score calculation and move index are based on this
        /// maximum.</remarks>
        /// <param name="cube">The initial state of the cube, represented as an array of integers.</param>
        /// <param name="cubeSolved">The solved state of the cube, represented as an array of integers. Used for comparison in certain evaluation
        /// functions.</param>
        /// <param name="movesSequence">An array of integers representing the sequence of moves to apply to the cube. If null or empty, the method
        /// returns an array of zeros.</param>
        /// <param name="typeFunc">An integer specifying the evaluation function to use. Valid values are: 1 (compare to solved state), 2
        /// (entropy), 3 (2x2x3 check), or 4 (combined 2-gen and 2x2x3 checks).</param>
        /// <returns>An array of two integers. The first element is the computed score (10 times the maximum evaluation value
        /// minus the move index), and the second element is the 1-based index of the move where the maximum evaluation
        /// was achieved. Returns [0, 0] if no moves are provided.</returns>
        public static int[] FObj(int[] cube, int[] cubeSolved, int[] movesSequence, int typeFunc)
        {
            var res = new int[2];
            if (movesSequence == null || movesSequence.Length == 0)
            {
                res[0] = 0;
                res[1] = 0;
                return res;
            }

            int nmax = 0;
            int x = 0;
            var cubeTemp = (int[])cube.Clone();

            for (int i = 0; i < movesSequence.Length; i++)
            {
                cubeTemp = DoMove(cubeTemp, movesSequence[i]);
                int ni = typeFunc switch
                {
                    1 => Compare(cubeTemp, cubeSolved),
                    2 => Entropy(cubeTemp),
                    3 => Is2x2x3(cubeTemp),
                    4 => 10 * Is2gen(cubeTemp) + 10 * Is2x2x3(cubeTemp),
                    _ => 0
                };

                if (ni > nmax)
                {
                    nmax = ni;
                    x = i + 1; // Fortran used 1-based index for x
                }
            }

            res[0] = 10 * nmax - x;
            res[1] = x;
            return res;
        }

        /// <summary>
        /// Reorders the rows of a two-dimensional array in descending order based on the corresponding values in a
        /// specified objective function array.
        /// </summary>
        /// <remarks>The returned array contains the same data as the input population array, but with
        /// rows rearranged so that the individual with the highest objective function value appears first. The lengths
        /// of the population and objective function arrays must match.</remarks>
        /// <param name="pop">The two-dimensional array representing the population, where each row corresponds to an individual.</param>
        /// <param name="valFobj">An array of objective function values, where each element corresponds to the fitness or score of the
        /// individual in the same row of the population array.</param>
        /// <returns>A new two-dimensional array with rows reordered so that individuals are sorted in descending order according
        /// to their objective function values.</returns>
        public static int[,] ClassementPopulation(int[,] pop, int[] valFobj)
        {
            int rows = pop.GetLength(0);
            int cols = pop.GetLength(1);
            var indices = Enumerable.Range(0, valFobj.Length)
                                    .Select(idx => new { idx, val = valFobj[idx] })
                                    .OrderByDescending(x => x.val)
                                    .Select(x => x.idx)
                                    .ToArray();

            var outArr = new int[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                int src = indices[i];
                for (int j = 0; j < cols; j++) outArr[i, j] = pop[src, j];
            }

            return outArr;
        }

        /// <summary>
        /// Performs a crossover operation on a population matrix using a faithful approximation algorithm, producing a
        /// new generation with elite preservation and probabilistic recombination.
        /// </summary>
        /// <remarks>The method preserves the elite individual (the first row of the input population) and
        /// fills the remaining population with offspring generated by crossover or direct copying, based on the
        /// specified probability. Parent selection is influenced by the selection pressure parameter, allowing for
        /// adjustable bias toward higher-performing individuals. The input arrays are not modified.</remarks>
        /// <param name="pop">The current population represented as a two-dimensional array, where each row corresponds to an individual
        /// and each column to a gene or feature.</param>
        /// <param name="phi">The selection pressure parameter that influences the probability distribution for parent selection. Must be
        /// greater than or equal to 1.0.</param>
        /// <param name="pc">The probability of performing crossover between selected parents. Must be between 0.0 and 1.0, inclusive.</param>
        /// <param name="perfo">A two-dimensional array representing the performance or fitness of each individual in the population. Used
        /// to inform selection, but not modified by this method.</param>
        /// <returns>A new two-dimensional array representing the next generation population after crossover. The first
        /// individual (elite) is preserved from the input population.</returns>
        public static int[,] PopMariee(int[,] pop, double phi, double pc, int[,] perfo)
        {
            int n = pop.GetLength(0);
            int m = pop.GetLength(1);
            var result = (int[,])pop.Clone();

            // compute pi array
            double[] pi = new double[n];
            for (int i = 0; i < n; i++)
            {
                pi[i] = 1.0 / n * (phi - i * (2.0 * phi - 2.0) / (n - 1));
            }

            // preserve elite (index 0)
            // fill remaining with children: start at index 1 (Fortran started at 2 because 1-based and kept elite)
            int childIdx = 1;
            while (childIdx < n && childIdx + 1 < n)
            {
                int p1 = 0, p2 = 0;
                // select first parent using rejection described
                while (true)
                {
                    double x0 = RandomDouble();
                    int r0 = (int)(n * 0.9999 * x0);
                    double y0 = RandomDouble();
                    if ((2.0 - phi + 2.0 * y0 * (phi - 1.0)) / n <= pi[r0])
                    {
                        p1 = r0;
                        break;
                    }
                }

                // second parent uniform
                p2 = (int)(n * 0.9999 * RandomDouble());

                // choose crossover point in {1..m-1}
                int cross = (int)((m - 1) * 0.9999 * RandomDouble()) + 1;
                if (cross == m) cross = m - 1;

                if (RandomDouble() < pc)
                {
                    // child A
                    for (int j = 0; j < cross; j++) result[childIdx, j] = pop[p1, j];
                    for (int j = cross; j < m; j++) result[childIdx, j] = pop[p2, j];
                    // child B
                    for (int j = 0; j < cross; j++) result[childIdx + 1, j] = pop[p2, j];
                    for (int j = cross; j < m; j++) result[childIdx + 1, j] = pop[p1, j];
                }
                else
                {
                    // no crossover
                    for (int j = 0; j < m; j++)
                    {
                        result[childIdx, j] = pop[p1, j];
                        result[childIdx + 1, j] = pop[p2, j];
                    }
                }

                childIdx += 2;
            }

            // If odd remaining slot, copy random parent
            if (childIdx < n)
            {
                int p = (int)(n * 0.9999 * RandomDouble());
                for (int j = 0; j < m; j++) result[childIdx, j] = pop[p, j];
            }

            return result;
        }

        /// <summary>
        /// Returns a mutated copy of the specified population matrix, applying random mutations to each non-elite
        /// individual with the given mutation probability.
        /// </summary>
        /// <remarks>Mutation is applied independently to each gene of non-elite individuals. For each
        /// gene selected for mutation, a new random integer value in the range [0, 17] is assigned. The original
        /// population array is not modified.</remarks>
        /// <param name="pop">A two-dimensional array representing the population, where each row corresponds to an individual and each
        /// column to a gene.</param>
        /// <param name="pm">The probability of mutating each gene. Must be between 0.0 and 1.0, inclusive.</param>
        /// <returns>A new two-dimensional array containing the mutated population. The first row (elite individual) is preserved
        /// without mutation.</returns>
        public static int[,] PopMutee(int[,] pop, double pm)
        {
            int n = pop.GetLength(0);
            int m = pop.GetLength(1);
            var res = (int[,])pop.Clone();

            for (int i = 1; i < n; i++) // start at 1 to preserve elite
            {
                for (int j = 0; j < m; j++)
                {
                    if (RandomDouble() < pm)
                    {
                        res[i, j] = (int)(18 * 0.9999 * RandomDouble());
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Reduces redundant or cancelling moves in a sequence while preserving the original array length, replacing
        /// removed moves with random moves as needed.
        /// </summary>
        /// <remarks>This method is typically used to simplify move sequences by eliminating pairs of
        /// moves that cancel each other or can be combined, while ensuring the output array remains the same length as
        /// the input. The replaced moves are filled with random values, which may affect downstream processing if the
        /// sequence is expected to be minimal or canonical.</remarks>
        /// <param name="movesSequence">An array of integers representing a sequence of moves to be trimmed. May be null.</param>
        /// <returns>A new array of the same length as the input, with redundant or cancelling moves removed and replaced by
        /// random moves. Returns an empty array if the input is null.</returns>
        public static int[] TrimSequence(int[] movesSequence)
        {
            if (movesSequence == null) return Array.Empty<int>();
            int ltot = movesSequence.Length;
            var seq = (int[])movesSequence.Clone();

            bool changed;
            do
            {
                changed = false;
                for (int l = 0; l < ltot - 1; l++)
                {
                    int move = seq[l];
                    int next = seq[l + 1];
                    if (move % 6 != next % 6) continue;

                    // Two moves cancel if abs difference == 12
                    if (Math.Abs(move - next) == 12)
                    {
                        // remove l and l+1 by shifting left and fill tail with randoms
                        for (int t = l; t < ltot - 2; t++) seq[t] = seq[t + 2];
                        seq[ltot - 2] = (int)(18 * 0.9999 * RandomDouble());
                        seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                        changed = true;
                        break;
                    }

                    // Now handle face / face2 / face' composition rules (keeps parity)
                    if (move < 6)
                    {
                        if (next == move)
                        {
                            seq[l] = move + 6;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                        else if (next == move + 6)
                        {
                            seq[l] = move + 12;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                    }

                    if (move < 12)
                    {
                        if (move < 6) { /* already handled */ }
                        else
                        {
                            if (next == move - 6)
                            {
                                seq[l] = move + 6;
                                for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                                seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                                changed = true;
                                break;
                            }
                            else if (next == move)
                            {
                                // face^2 then same => cancels
                                for (int t = l; t < ltot - 2; t++) seq[t] = seq[t + 2];
                                seq[ltot - 2] = (int)(18 * 0.9999 * RandomDouble());
                                seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                                changed = true;
                                break;
                            }
                            else if (next == move + 6)
                            {
                                seq[l] = move - 6;
                                for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                                seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                                changed = true;
                                break;
                            }
                        }
                    }

                    // move >= 12 (face')
                    if (move >= 12)
                    {
                        if (next == move - 6)
                        {
                            seq[l] = move - 12;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                        else if (next == move)
                        {
                            seq[l] = move - 6;
                            for (int t = l + 1; t < ltot - 1; t++) seq[t] = seq[t + 1];
                            seq[ltot - 1] = (int)(18 * 0.9999 * RandomDouble());
                            changed = true;
                            break;
                        }
                    }
                }
            } while (changed);

            return seq;
        }

        /// <summary>
        /// Transforms a sequence of move identifiers into their corresponding twisted move representations.
        /// </summary>
        /// <remarks>This method is typically used in contexts where move identifiers require
        /// normalization or transformation to a specific twisted form. The length of the returned array matches the
        /// input array.</remarks>
        /// <param name="movesSequence">An array of integers representing the original sequence of move identifiers to be transformed. If null, an
        /// empty array is returned.</param>
        /// <returns>An array of integers containing the twisted move representations corresponding to each input move. Returns
        /// an empty array if <paramref name="movesSequence"/> is null.</returns>
        public static int[] TwistSequence(int[] movesSequence)
        {
            if (movesSequence == null) return Array.Empty<int>();
            var res = new int[movesSequence.Length];
            for (int i = 0; i < movesSequence.Length; i++)
            {
                int m = movesSequence[i];
                // formula from Fortran: 6*(m/6) + mod(1+m-6*(m/6),3) + 3*((m-6*(m/6))/3)
                int baseFace = (m / 6) * 6;
                int part1 = (1 + m - baseFace) % 3;
                int part2 = 3 * ((m - baseFace) / 3);
                res[i] = baseFace + (part1 + part2);
            }
            return res;
        }

        /// <summary>
        /// Returns a new array representing the cube state after applying a specific twist transformation to the input
        /// cube state.
        /// </summary>
        /// <remarks>The transformation rearranges the stickers of the cube according to a predefined
        /// mapping, simulating a particular twist. The input array is not modified.</remarks>
        /// <param name="c">An array of 54 integers representing the current state of the cube, where each element corresponds to a
        /// sticker position. The array must have a length of 54.</param>
        /// <returns>A new array of 54 integers representing the cube state after the twist transformation has been applied.</returns>
        public static int[] TwistCube(int[] c)
        {
            var t = new int[54];
            // F <- R
            t[0] = c[24]; t[1] = c[21]; t[2] = c[18];
            t[3] = c[25]; t[4] = c[22]; t[5] = c[19];
            t[6] = c[26]; t[7] = c[23]; t[8] = c[20];
            // U <- F
            t[9] = c[6]; t[10] = c[3]; t[11] = c[0];
            t[12] = c[7]; t[13] = c[4]; t[14] = c[1];
            t[15] = c[8]; t[16] = c[5]; t[17] = c[2];
            // R <- U
            t[18] = c[17]; t[19] = c[16]; t[20] = c[15];
            t[21] = c[14]; t[22] = c[13]; t[23] = c[12];
            t[24] = c[11]; t[25] = c[10]; t[26] = c[9];
            // B <- L
            t[27] = c[47]; t[28] = c[50]; t[29] = c[53];
            t[30] = c[46]; t[31] = c[49]; t[32] = c[52];
            t[33] = c[45]; t[34] = c[48]; t[35] = c[51];
            // D <- B
            t[36] = c[33]; t[37] = c[30]; t[38] = c[27];
            t[39] = c[34]; t[40] = c[31]; t[41] = c[28];
            t[42] = c[35]; t[43] = c[32]; t[44] = c[29];
            // L <- D
            for (int i = 45; i <= 53; i++) t[i] = c[36 + (i - 45)];
            return t;
        }

       /// <summary>
       /// Returns a new array representing the cube state after rotating the entire cube clockwise around the vertical
       /// axis.
       /// </summary>
       /// <remarks>This method assumes the input array uses a standard sticker ordering for a 3x3x3 cube.
       /// The rotation affects all faces as if the entire cube is turned clockwise when viewed from above.</remarks>
       /// <param name="cu">An array of 54 integers representing the current state of the cube, where each element corresponds to a
       /// sticker on the cube. The array must have a length of 54.</param>
       /// <returns>A new array of 54 integers representing the cube state after the rotation. The original array is not
       /// modified.</returns>
        public static int[] RotateWholeCube(int[] cu)
        {
            var t = new int[54];
            // U
            t[9] = cu[15]; t[10] = cu[12]; t[11] = cu[9];
            t[12] = cu[16]; t[13] = cu[13]; t[14] = cu[10];
            t[15] = cu[17]; t[16] = cu[14]; t[17] = cu[11];
            // D
            t[36] = cu[38]; t[37] = cu[41]; t[38] = cu[44];
            t[39] = cu[37]; t[40] = cu[40]; t[41] = cu[43];
            t[42] = cu[36]; t[43] = cu[39]; t[44] = cu[42];
            // F <- R
            for (int i = 0; i < 9; i++) t[i] = cu[18 + i];
            // R <- B
            for (int i = 0; i < 9; i++) t[18 + i] = cu[27 + i];
            // B <- L
            for (int i = 0; i < 9; i++) t[27 + i] = cu[45 + i];
            // L <- F
            for (int i = 0; i < 9; i++) t[45 + i] = cu[i];
            return t;
        }

        /// <summary>
        /// Rotates the entire cube state around the X-axis (left-right axis) and returns the resulting state.
        /// </summary>
        /// <remarks>This method first applies a twist to the cube state, then performs three additional
        /// whole-cube rotations to achieve the equivalent of an X-axis rotation. The input array is not modified; a new
        /// array is returned.</remarks>
        /// <param name="cu">An array representing the current state of the cube. The format and length must match the expected cube
        /// state representation.</param>
        /// <returns>An array representing the cube state after rotation around the X-axis.</returns>
        public static int[] RotateWholeCubeX(int[] cu)
        {
            // rotate around LR axis: twist_cube then 3 * rotate_whole_cube
            var res = TwistCube(cu);
            for (int rot = 1; rot <= 3; rot++) res = RotateWholeCube(res);
            return res;
        }

        /// <summary>
        /// Attempts to find a solution to transform a given 2-generator Rubik's Cube state into the solved state using only
        /// allowed moves (F and U face turns and their variants) via brute-force search.
        /// </summary>
        /// <remarks>This method performs an iterative deepening brute-force search to find a sequence of allowed moves
        /// that solves the cube. The allowed moves are limited to F and U face turns and their variants. The search depth is
        /// capped to prevent excessive computation time. If a solution is found, it is written to the console and appended to a
        /// results file. If the cube is already solved, a message is logged. If no solution is found within the maximum depth,
        /// this is also reported. This method returns the number of moves found (0 means cube already solved, -1 means no solution).</remarks>
        /// <param name="cube2gen">An array representing the initial state of the cube, restricted to moves generated by two faces. Cannot be null.</param>
        /// <param name="cubeSolved">An array representing the target solved state of the cube. Cannot be null.</param>
        /// <returns>The number of moves in the found solution (>0), 0 if the cube was already solved, or -1 if no solution was found.</returns>
        public static int SolveTwogenBourrin(int[] cube2gen, int[] cubeSolved)
        {
            int foundDepth = -1;
            if (cube2gen == null || cubeSolved == null) return foundDepth;

            // Allowed moves in 2-gen (F and U, with variants): 0,1,6,7,12,13
            int[] allowedMoves = new int[] { 0, 1, 6, 7, 12, 13 };

            // Maximum search depth (configurable). Beware: complexity ~ 3^depth
            const int maxDepth = 15;

            int[] currentSeq = new int[maxDepth];

            // Helper recursive DFS (depth-limited). We avoid consecutive moves on same face (move%6)
            bool Dfs(int[] cubeState, int depthLeft, int lastFace, int pos)
            {
                if (Compare(cubeState, cubeSolved) == 54)
                {
                    foundDepth = pos;
                    return true;
                }

                if (depthLeft == 0) return false;

                foreach (var move in allowedMoves)
                {
                    int face = move % 6;
                    if (lastFace != -1 && face == lastFace) continue; // prune consecutive same face

                    currentSeq[pos] = move;
                    var nextCube = DoMove(cubeState, move);

                    if (Dfs(nextCube, depthLeft - 1, face, pos + 1)) return true;
                }

                return false;
            }

            // Iterative deepening
            for (int depth = 0; depth <= maxDepth && foundDepth == -1; depth++)
            {
                Console.WriteLine("Brute force depth : {0}", depth);
                var startCube = (int[])cube2gen.Clone();
                if (Dfs(startCube, depth, -1, 0))
                {
                    break;
                }
            }

            if (foundDepth > 0)
            {
                var solution = currentSeq.Take(foundDepth).ToArray();
                var notation = ToNotation(solution);
                Console.WriteLine("Yay! (bourrin) solution found");
                Console.WriteLine(string.Join("", notation));
                try
                {
                    System.IO.File.AppendAllText("results.txt", string.Join("", notation) +"//SolveTwogenBourrin" + Environment.NewLine);
                }
                catch
                {
                    // ignore IO errors
                }
            }
            else if (foundDepth == 0)
            {
                Console.WriteLine("Cube already solved (bourrin).");
                try
                {
                    System.IO.File.AppendAllText("results.txt", "//SolveTwogenBourrin : cube already solved" + Environment.NewLine);
                }
                catch { }
            }
            else
            {
                Console.WriteLine($"SolveTwogenBourrin: no solution found up to depth {maxDepth}.");
            }
            return foundDepth;
        }
    }
}