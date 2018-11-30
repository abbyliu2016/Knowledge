namespace ConsoleApp1
{
    using System;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;

    // Demonstrates a basic producer and consumer pattern that uses dataflow.
    class DataflowProducerConsumer
    {
        // Demonstrates the production end of the producer and consumer pattern.
        public static void Produce(ITargetBlock<byte[]> target)
        {
            // Create a Random object to generate random data.
            Random rand = new Random();

            // In a loop, fill a buffer with random data and
            // post the buffer to the target block.
            for (int i = 0; i < 100; i++)
            {
                // Create an array to hold random byte data.
                byte[] buffer = new byte[1024];

                // Fill the buffer with random bytes.
                rand.NextBytes(buffer);

                // Post the result to the message block.
                target.Post(buffer);
            }

            // Set the target to the completed state to signal to the consumer
            // that no more data will be available.
            target.Complete();
        }

        // Demonstrates the consumption end of the producer and consumer pattern.
        public static async Task<int> ConsumeAsync(IReceivableSourceBlock<byte[]> source)
        {
            // Initialize a counter to track the number of bytes that are processed.
            int bytesProcessed = 0;

            // Read from the source buffer until the source buffer has no 
            // available output data.
            while (await source.OutputAvailableAsync())
            {
                byte[] data;

                // We use IReceivableSourceBlock rather than ISourceBlock and uses the TryReceive rather than Receive to make sure the data is still available when multiple consumers are there
                while (source.TryReceive(out data))
                {
                    // Increment the count of bytes received.
                    bytesProcessed += data.Length;
                }
            }

            return bytesProcessed;
        }

}
