using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create a BufferBlock<byte[]> object. This object serves as the 
            // target block for the producer and the source block for the consumer.
            var buffer = new BufferBlock<byte[]>();

            // Start the consumer. The Consume method runs asynchronously. 
            var consumer = DataflowProducerConsumer.ConsumeAsync(buffer);

            // Post source data to the dataflow block.
            DataflowProducerConsumer.Produce(buffer);

            // Wait for the consumer to process all data.
            consumer.Wait();

            // Print the count of bytes processed to the console.
            Console.WriteLine("Processed {0} bytes.", consumer.Result);
        }
    }
}
