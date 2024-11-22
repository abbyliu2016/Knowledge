using System.Collections;

public partial class JsonFileIterator
{
    public class MultiFileIterator : IEnumerable<string>, IEnumerator<string>
    {
        private readonly List<string> _filePaths;
        private FileStream _currentFileStream;
        private StreamReader _currentReader;
        private string _currentLine;
        private int _currentFileIndex = -1;
        private long _currentPosition = 0; // To track position in the current file

        public MultiFileIterator(List<string> filePaths)
        {
            _filePaths = filePaths;
        }

        public string Current => _currentLine;

        object IEnumerator.Current => _currentLine;

        // Move to the next line in the current file or switch to the next file
        public bool MoveNext()
        {
            if (_currentReader == null || _currentReader.EndOfStream)
            {
                if (!MoveToNextFile())
                    return false; // No more files to process
            }

            // Read the next line from the current file
            _currentLine = _currentReader.ReadLine();
            _currentPosition = _currentFileStream.Position - _currentReader.BaseStream.Length + _currentReader.CurrentEncoding.GetByteCount(_currentLine + Environment.NewLine); // Update position after reading
                                                                                                                                                                                 //_currentPosition = _currentFileStream.Position; // Update position after reading
            return _currentLine != null;
        }

        // Move to the next file and initialize StreamReader
        private bool MoveToNextFile()
        {
            DisposeCurrentFileStream(); // Dispose previous file streams if any

            _currentFileIndex++;
            if (_currentFileIndex >= _filePaths.Count)
                return false; // No more files to process

            // Open a new FileStream for the next file and seek to the saved position
            _currentFileStream = new FileStream(_filePaths[_currentFileIndex], FileMode.Open, FileAccess.Read, FileShare.Read);
            _currentReader = new StreamReader(_currentFileStream);

            // If we're reopening the file, we need to seek to the saved position
            if (_currentPosition > 0)
            {
                _currentFileStream.Seek(_currentPosition, SeekOrigin.Begin);
                _currentReader.DiscardBufferedData(); // Reset the reader's internal buffer to account for the new position
            }

            return true;
        }

        // Save the current position (for saving/restoring cursor position)
        public long GetCurrentPosition()
        {
            return _currentPosition;
        }

        // Restore the position within the current file (can be used for resuming from a saved position)
        public void SetPosition(long position)
        {
            _currentPosition = position;
        }

        // Reset the iterator to the beginning
        public void Reset()
        {
            Dispose();
            _currentFileIndex = -1;
            _currentPosition = 0;
        }

        // Dispose of the current file streams and readers
        private void DisposeCurrentFileStream()
        {
            _currentReader?.Dispose();
            _currentFileStream?.Dispose();
        }

        // Dispose everything
        public void Dispose()
        {
            DisposeCurrentFileStream();
        }

        public IEnumerator<string> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
