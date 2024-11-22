using System.Collections;

public partial class JsonFileIterator
{
    public class MultiFileIterator1 : IEnumerator<string>//, IEnumerable<string>
    {
        private List<string> filePaths;
        private int currentIndex;
        private string currenLine;

        private List<int> positions;
        private List<bool> end;

        private FileStream currentFileStream;
        private StreamReader currentStreamReader;

        public MultiFileIterator1(List<string> filePaths)
        {
            this.filePaths = filePaths;
            this.currentIndex = -1;
            this.positions = new List<int>(filePaths.Count);
            this.end = new List<bool>(filePaths.Count);
            for (int i = 0; i < filePaths.Count; i++)
            {
                positions.Add(0);
                this.end.Add(false);
            }
        }

        public string Current => currenLine;

        object IEnumerator.Current => currenLine;

        public void Dispose()
        {
            currentFileStream?.Dispose();
            currentStreamReader?.Dispose();
        }



        public bool MoveNext()
        {
            if (!MoveToNextFile())
            {
                return false;
            }

            currenLine = currentStreamReader.ReadLine();
            if (currenLine == null)
            {
                return MoveNext();
            }

            positions[currentIndex] += (int)currentStreamReader.CurrentEncoding.GetByteCount(currenLine + Environment.NewLine); // Update position after reading;
            if (positions[currentIndex] >= currentFileStream.Length)
            {
                end[currentIndex] = true;
            }
            return true;
        }

        private bool MoveToNextFile()
        {
            currentFileStream?.Dispose();
            currentStreamReader?.Dispose();
            int endCount = 0;
            currentIndex = (currentIndex + 1) % filePaths.Count;
            while (end[currentIndex] == true)
            {

                endCount++;

                if (endCount == filePaths.Count)
                {
                    return false;
                }
                currentIndex = (currentIndex + 1) % filePaths.Count;
            }

            currentFileStream = new FileStream(filePaths[currentIndex], FileMode.Open, FileAccess.Read);
            currentFileStream.Seek(positions[currentIndex], SeekOrigin.Begin);
            currentStreamReader = new StreamReader(currentFileStream, bufferSize: 500);
            return true;
        }


        public void Reset()
        {
            Dispose();
            currentIndex = -1;
            for (int i = 0; i < positions.Count; i++)
            {
                positions[i] = 0;
            }
        }

        
    }
}
