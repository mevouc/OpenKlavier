using System.Text;

namespace Klavier.SoundFont.Parsing;

// Minimal SF2 parser: extracts metadata from a SoundFont 2 file.
// SF2 is a RIFF file (form type "sfbk"). Preset headers live in the "pdta" LIST -> "phdr" sub-chunk.
// Bank display name lives in the "INFO" LIST -> "INAM" sub-chunk.
public static class SoundFontParser
{
    private const int _PhdrRecordSize = 38;
    private const int _PresetNameLength = 20;

    // Opens the file once and extracts both bank name (INAM) and preset list (phdr) in a single pass.
    // Throws InvalidDataException if the file is not a valid SoundFont or is missing the mandatory pdta LIST.
    // Returns Name = null when INAM is absent or empty (valid SF2 without bank-name metadata).
    public static SoundFontInfo ParseInfo(string filePath)
    {
        using BinaryReader reader = OpenSoundFontReader(filePath);
        long fileEnd = reader.BaseStream.Length;

        string? name = null;
        IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset>? presets = null;

        while (reader.BaseStream.Position < fileEnd && (name is null || presets is null))
        {
            string chunkId = ReadFourCC(reader);
            uint size = reader.ReadUInt32();

            if (chunkId != "LIST")
            {
                reader.BaseStream.Seek(size, SeekOrigin.Current);
                continue;
            }

            string listType = ReadFourCC(reader);
            long listEnd = reader.BaseStream.Position + size - 4;

            if (listType == "INFO" && name is null)
            {
                name = ReadInamName(reader, listEnd);
            }
            else if (listType == "pdta" && presets is null)
            {
                presets = ReadPresets(reader, listEnd);
            }

            reader.BaseStream.Position = listEnd;
        }

        if (presets is null)
        {
            throw new InvalidDataException($"pdta LIST not found in {filePath}");
        }

        return new SoundFontInfo(name, presets);
    }

    private static string? ReadInamName(BinaryReader reader, long listEnd)
    {
        try
        {
            long inamSize = SkipToSubChunk(reader, "INAM", listEnd);
            byte[] nameBytes = reader.ReadBytes((int)inamSize);
            string value = ReadNullTerminated(nameBytes);
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    private static IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> ReadPresets(BinaryReader reader, long listEnd)
    {
        long phdrSize = SkipToSubChunk(reader, "phdr", listEnd);

        if (phdrSize % _PhdrRecordSize != 0)
        {
            throw new InvalidDataException($"Invalid phdr chunk size: {phdrSize}");
        }

        int recordCount = (int)(phdrSize / _PhdrRecordSize);
        Dictionary<(int Bank, int Program), SoundFontPreset> presets = new(recordCount);

        for (int i = 0; i < recordCount; i++)
        {
            byte[] nameBytes = reader.ReadBytes(_PresetNameLength);
            int program = reader.ReadUInt16();
            int bank = reader.ReadUInt16();
            reader.ReadBytes(_PhdrRecordSize - _PresetNameLength - 4); // bag index + library + genre + morphology

            // Last record is a sentinel terminator (EOP)
            if (i == recordCount - 1)
            {
                break;
            }

            // SF2/SF3 spec mandates unique (bank, program); on the rare malformed file with
            // duplicates, last write wins (matches FluidSynth's ProgramSelect resolution).
            presets[(bank, program)] = new SoundFontPreset(bank, program, ReadNullTerminated(nameBytes));
        }

        return presets;
    }

    private static BinaryReader OpenSoundFontReader(string filePath)
    {
        BinaryReader reader = new(File.OpenRead(filePath));
        try
        {
            if (ReadFourCC(reader) != "RIFF")
            {
                throw new InvalidDataException($"Not a RIFF file: {filePath}");
            }
            reader.ReadUInt32();
            if (ReadFourCC(reader) != "sfbk")
            {
                throw new InvalidDataException($"Not a SoundFont (sfbk) file: {filePath}");
            }
            return reader;
        }
        catch
        {
            reader.Dispose();
            throw;
        }
    }

    private static string ReadFourCC(BinaryReader reader)
        => Encoding.ASCII.GetString(reader.ReadBytes(4));

    private static long SkipToSubChunk(BinaryReader reader, string subChunkId, long listEnd)
    {
        while (reader.BaseStream.Position < listEnd)
        {
            string chunkId = ReadFourCC(reader);
            uint size = reader.ReadUInt32();

            if (chunkId == subChunkId)
            {
                return size;
            }
            reader.BaseStream.Seek(size, SeekOrigin.Current);
        }
        throw new InvalidDataException($"Sub-chunk '{subChunkId}' not found");
    }

    private static string ReadNullTerminated(byte[] bytes)
    {
        int len = Array.IndexOf(bytes, (byte)0);
        return Encoding.ASCII.GetString(bytes, 0, len < 0 ? bytes.Length : len);
    }
}
