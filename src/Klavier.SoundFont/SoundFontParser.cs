using System.Text;

namespace Klavier.SoundFont;

// Minimal SF2 parser: extracts the preset list (PHDR sub-chunk) from a SoundFont 2 file.
// SF2 is a RIFF file (form type "sfbk") containing a "pdta" LIST whose "phdr" sub-chunk
// holds 38-byte preset header records, the last being a sentinel terminator.
public static class SoundFontParser
{
    private const int _PhdrRecordSize = 38;
    private const int _PresetNameLength = 20;

    public static IReadOnlyDictionary<(int Bank, int Program), SoundFontPreset> ParsePresets(string filePath)
    {
        using FileStream stream = File.OpenRead(filePath);
        using BinaryReader reader = new(stream);

        if (ReadFourCC(reader) != "RIFF")
        {
            throw new InvalidDataException($"Not a RIFF file: {filePath}");
        }
        reader.ReadUInt32();
        if (ReadFourCC(reader) != "sfbk")
        {
            throw new InvalidDataException($"Not a SoundFont (sfbk) file: {filePath}");
        }

        long pdtaEnd = SkipToListChunk(reader, "pdta", stream.Length);
        long phdrSize = SkipToSubChunk(reader, "phdr", pdtaEnd);

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

    private static string ReadFourCC(BinaryReader reader)
        => Encoding.ASCII.GetString(reader.ReadBytes(4));

    private static long SkipToListChunk(BinaryReader reader, string listType, long fileEnd)
    {
        while (reader.BaseStream.Position < fileEnd)
        {
            string chunkId = ReadFourCC(reader);
            uint size = reader.ReadUInt32();

            if (chunkId == "LIST")
            {
                string type = ReadFourCC(reader);
                if (type == listType)
                {
                    return reader.BaseStream.Position + size - 4;
                }
                reader.BaseStream.Seek(size - 4, SeekOrigin.Current);
            }
            else
            {
                reader.BaseStream.Seek(size, SeekOrigin.Current);
            }
        }
        throw new InvalidDataException($"LIST '{listType}' not found");
    }

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
