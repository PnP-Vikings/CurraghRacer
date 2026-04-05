using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AwesomeTaskManager.UI
{
    /// <summary>
    /// Minimal GIF decoder that extracts frames and delays for animated GIF display in the editor.
    /// </summary>
    public class GifDecoder
    {
        public struct GifFrame
        {
            public Texture2D texture;
            public float delay; // seconds
        }

        private GifFrame[] _frames;
        private float _totalDuration;

        public GifFrame[] Frames => _frames;
        public int FrameCount => _frames?.Length ?? 0;
        public float TotalDuration => _totalDuration;

        /// <summary>Maximum number of frames to decode. Each frame is one Texture2D in memory.</summary>
        public const int MaxFrames = 200;
        /// <summary>Maximum file size in bytes (50 MB).</summary>
        public const long MaxFileSize = 50 * 1024 * 1024;

        public static GifDecoder Load(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var info = new FileInfo(path);
                if (info.Length > MaxFileSize)
                {
                    Debug.LogWarning($"[GifDecoder] GIF too large ({info.Length / (1024 * 1024f):F1} MB, max {MaxFileSize / (1024 * 1024)} MB): {path}");
                    return null;
                }
                byte[] data = File.ReadAllBytes(path);
                return Parse(data);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[GifDecoder] Failed to parse GIF: {e.Message}");
                return null;
            }
        }

        public static GifDecoder LoadFromAssetPath(string assetPath)
        {
            string fullPath = assetPath;
            if (assetPath.StartsWith("Assets"))
                fullPath = Path.Combine(Application.dataPath, "..", assetPath);
            return Load(fullPath);
        }

        /// <summary>Get the appropriate frame index for the current time.</summary>
        public int GetFrameIndex(double time)
        {
            if (_frames == null || _frames.Length <= 1) return 0;
            float t = (float)(time % _totalDuration);
            float accum = 0f;
            for (int i = 0; i < _frames.Length; i++)
            {
                accum += _frames[i].delay;
                if (t < accum) return i;
            }
            return _frames.Length - 1;
        }

        // ════════════════════════════════════════════
        //  GIF PARSER
        // ════════════════════════════════════════════

        private static GifDecoder Parse(byte[] data)
        {
            if (data.Length < 13) return null;

            // Header
            string sig = System.Text.Encoding.ASCII.GetString(data, 0, 6);
            if (sig != "GIF87a" && sig != "GIF89a") return null;

            int pos = 6;

            // Logical Screen Descriptor
            int screenWidth = BitConverter.ToUInt16(data, pos); pos += 2;
            int screenHeight = BitConverter.ToUInt16(data, pos); pos += 2;
            byte packed = data[pos++];
            byte bgColor = data[pos++];
            pos++; // pixel aspect ratio

            bool hasGlobalTable = (packed & 0x80) != 0;
            int globalTableSize = 1 << ((packed & 0x07) + 1);

            Color32[] globalPalette = null;
            if (hasGlobalTable)
            {
                globalPalette = ReadColorTable(data, ref pos, globalTableSize);
            }

            var frames = new List<GifFrame>();
            float currentDelay = 0.1f;
            int disposalMethod = 0;
            bool hasTransparency = false;
            int transparentIndex = 0;

            // Canvas for compositing
            Color32[] canvas = new Color32[screenWidth * screenHeight];
            Color32[] prevCanvas = new Color32[screenWidth * screenHeight];

            // Initialize with background
            Color32 bgCol = (globalPalette != null && bgColor < globalPalette.Length)
                ? globalPalette[bgColor]
                : new Color32(0, 0, 0, 0);
            for (int i = 0; i < canvas.Length; i++) canvas[i] = bgCol;

            while (pos < data.Length)
            {
                byte blockType = data[pos++];

                if (blockType == 0x3B) // Trailer
                    break;

                if (blockType == 0x21) // Extension
                {
                    if (pos >= data.Length) break;
                    byte extLabel = data[pos++];

                    if (extLabel == 0xF9) // Graphics Control Extension
                    {
                        pos++; // block size (always 4)
                        byte gcPacked = data[pos++];
                        disposalMethod = (gcPacked >> 2) & 0x07;
                        hasTransparency = (gcPacked & 0x01) != 0;
                        int delayCs = BitConverter.ToUInt16(data, pos); pos += 2;
                        currentDelay = delayCs <= 0 ? 0.1f : delayCs / 100f;
                        transparentIndex = data[pos++];
                        pos++; // block terminator
                    }
                    else
                    {
                        // Skip other extensions
                        SkipSubBlocks(data, ref pos);
                    }
                    continue;
                }

                if (blockType == 0x2C) // Image Descriptor
                {
                    // Stop if we've hit the frame limit
                    if (frames.Count >= MaxFrames)
                        break;
                    int imgLeft = BitConverter.ToUInt16(data, pos); pos += 2;
                    int imgTop = BitConverter.ToUInt16(data, pos); pos += 2;
                    int imgWidth = BitConverter.ToUInt16(data, pos); pos += 2;
                    int imgHeight = BitConverter.ToUInt16(data, pos); pos += 2;
                    byte imgPacked = data[pos++];

                    bool hasLocalTable = (imgPacked & 0x80) != 0;
                    bool interlaced = (imgPacked & 0x40) != 0;
                    int localTableSize = 1 << ((imgPacked & 0x07) + 1);

                    Color32[] palette = globalPalette;
                    if (hasLocalTable)
                        palette = ReadColorTable(data, ref pos, localTableSize);

                    if (palette == null) break;

                    // Save previous canvas for disposal
                    Array.Copy(canvas, prevCanvas, canvas.Length);

                    // LZW decompress
                    int lzwMinCode = data[pos++];
                    byte[] indices = DecompressLzw(data, ref pos, lzwMinCode, imgWidth * imgHeight);

                    if (indices != null)
                    {
                        // Draw frame onto canvas
                        int[] rowOrder = GetRowOrder(imgHeight, interlaced);
                        for (int y = 0; y < imgHeight; y++)
                        {
                            int srcRow = rowOrder != null ? rowOrder[y] : y;
                            for (int x = 0; x < imgWidth; x++)
                            {
                                int srcIdx = srcRow * imgWidth + x;
                                if (srcIdx >= indices.Length) continue;
                                int palIdx = indices[srcIdx];

                                if (hasTransparency && palIdx == transparentIndex)
                                    continue; // keep existing pixel

                                int cx = imgLeft + x;
                                // Flip Y for Unity (GIF is top-down, Unity is bottom-up)
                                int cy = (screenHeight - 1) - (imgTop + y);
                                if (cx >= 0 && cx < screenWidth && cy >= 0 && cy < screenHeight)
                                {
                                    canvas[cy * screenWidth + cx] = palIdx < palette.Length
                                        ? palette[palIdx]
                                        : new Color32(0, 0, 0, 255);
                                }
                            }
                        }

                        // Create texture from canvas
                        var tex = new Texture2D(screenWidth, screenHeight, TextureFormat.RGBA32, false);
                        tex.SetPixels32(canvas);
                        tex.Apply();
                        tex.hideFlags = HideFlags.DontSave;
                        tex.filterMode = FilterMode.Point;

                        frames.Add(new GifFrame { texture = tex, delay = currentDelay });
                    }
                    else
                    {
                        // Skip remaining sub-blocks if decompression failed
                        SkipSubBlocks(data, ref pos);
                    }

                    // Apply disposal
                    switch (disposalMethod)
                    {
                        case 2: // Restore to background
                            for (int y = imgTop; y < imgTop + imgHeight && y < screenHeight; y++)
                            for (int x = imgLeft; x < imgLeft + imgWidth && x < screenWidth; x++)
                            {
                                int cy = (screenHeight - 1) - y;
                                canvas[cy * screenWidth + x] = bgCol;
                            }
                            break;
                        case 3: // Restore to previous
                            Array.Copy(prevCanvas, canvas, canvas.Length);
                            break;
                        // 0,1: do nothing (leave canvas as-is)
                    }

                    // Reset per-frame state
                    currentDelay = 0.1f;
                    disposalMethod = 0;
                    hasTransparency = false;
                    transparentIndex = 0;
                    continue;
                }

                // Unknown block, skip
                break;
            }

            if (frames.Count == 0) return null;

            var decoder = new GifDecoder();
            decoder._frames = frames.ToArray();
            decoder._totalDuration = 0;
            foreach (var f in decoder._frames)
                decoder._totalDuration += f.delay;
            if (decoder._totalDuration <= 0) decoder._totalDuration = 1f;

            return decoder;
        }

        private static Color32[] ReadColorTable(byte[] data, ref int pos, int count)
        {
            var table = new Color32[count];
            for (int i = 0; i < count; i++)
            {
                if (pos + 2 >= data.Length) { pos = data.Length; break; }
                table[i] = new Color32(data[pos], data[pos + 1], data[pos + 2], 255);
                pos += 3;
            }
            return table;
        }

        private static void SkipSubBlocks(byte[] data, ref int pos)
        {
            while (pos < data.Length)
            {
                int blockSize = data[pos++];
                if (blockSize == 0) break;
                pos += blockSize;
            }
        }

        private static int[] GetRowOrder(int height, bool interlaced)
        {
            if (!interlaced) return null;
            int[] order = new int[height];
            int idx = 0;
            // Pass 1: rows 0, 8, 16, ...
            for (int r = 0; r < height; r += 8) order[idx++] = r;
            // Pass 2: rows 4, 12, 20, ...
            for (int r = 4; r < height; r += 8) order[idx++] = r;
            // Pass 3: rows 2, 6, 10, ...
            for (int r = 2; r < height; r += 4) order[idx++] = r;
            // Pass 4: rows 1, 3, 5, ...
            for (int r = 1; r < height; r += 2) order[idx++] = r;

            // Build inverse: for output row i, which source row?
            // Actually we need: row[interlacedIndex] = sourceRow
            // The order array maps output-index → actual-row
            // We need inverse for SetPixels: for each actual-row, what's its position in the decompressed data
            // Actually our `order` is correct: order[i] = the row number for the i-th row in the compressed data
            return order;
        }

        // ════════════════════════════════════════════
        //  LZW DECOMPRESSOR
        // ════════════════════════════════════════════

        private static byte[] DecompressLzw(byte[] data, ref int pos, int minCodeSize, int pixelCount)
        {
            // Read all sub-blocks into a single byte stream
            var compressedStream = new List<byte>();
            while (pos < data.Length)
            {
                int blockSize = data[pos++];
                if (blockSize == 0) break;
                if (pos + blockSize > data.Length) break;
                for (int i = 0; i < blockSize; i++)
                    compressedStream.Add(data[pos++]);
            }

            if (compressedStream.Count == 0) return null;

            byte[] compressed = compressedStream.ToArray();
            int clearCode = 1 << minCodeSize;
            int eofCode = clearCode + 1;

            int codeSize = minCodeSize + 1;
            int codeMask = (1 << codeSize) - 1;
            int nextCode = eofCode + 1;
            int maxCode = 1 << codeSize;

            // Initialize code table
            const int MAX_TABLE = 4096;
            int[][] codeTable = new int[MAX_TABLE][];
            for (int i = 0; i < clearCode; i++)
                codeTable[i] = new[] { i };

            var output = new List<byte>(pixelCount);
            int bitBuffer = 0;
            int bitsInBuffer = 0;
            int byteIdx = 0;

            int prevCode = -1;

            while (output.Count < pixelCount)
            {
                // Read next code
                while (bitsInBuffer < codeSize && byteIdx < compressed.Length)
                {
                    bitBuffer |= compressed[byteIdx++] << bitsInBuffer;
                    bitsInBuffer += 8;
                }
                if (bitsInBuffer < codeSize) break;

                int code = bitBuffer & codeMask;
                bitBuffer >>= codeSize;
                bitsInBuffer -= codeSize;

                if (code == eofCode) break;

                if (code == clearCode)
                {
                    codeSize = minCodeSize + 1;
                    codeMask = (1 << codeSize) - 1;
                    nextCode = eofCode + 1;
                    maxCode = 1 << codeSize;
                    prevCode = -1;

                    // Re-initialize table
                    for (int i = clearCode + 2; i < MAX_TABLE; i++)
                        codeTable[i] = null;
                    continue;
                }

                int[] entry;
                if (code < nextCode && codeTable[code] != null)
                {
                    entry = codeTable[code];
                }
                else if (code == nextCode && prevCode >= 0 && codeTable[prevCode] != null)
                {
                    var prev = codeTable[prevCode];
                    entry = new int[prev.Length + 1];
                    Array.Copy(prev, entry, prev.Length);
                    entry[prev.Length] = prev[0];
                }
                else
                {
                    break; // invalid
                }

                for (int i = 0; i < entry.Length && output.Count < pixelCount; i++)
                    output.Add((byte)entry[i]);

                if (prevCode >= 0 && nextCode < MAX_TABLE && codeTable[prevCode] != null)
                {
                    var prev = codeTable[prevCode];
                    var newEntry = new int[prev.Length + 1];
                    Array.Copy(prev, newEntry, prev.Length);
                    newEntry[prev.Length] = entry[0];
                    codeTable[nextCode] = newEntry;
                    nextCode++;

                    if (nextCode >= maxCode && codeSize < 12)
                    {
                        codeSize++;
                        codeMask = (1 << codeSize) - 1;
                        maxCode = 1 << codeSize;
                    }
                }

                prevCode = code;
            }

            return output.Count > 0 ? output.ToArray() : null;
        }
    }
}


