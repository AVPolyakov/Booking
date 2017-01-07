using System;
using Ploeh.Samples.Booking.Persistence.FileSystem;
using System.IO;
using Ploeh.Samples.Booking.PersistenceModel;
using System.Threading;
using Ploeh.Samples.Booking.DomainModel;
using System.Threading.Tasks;
using Ploeh.Samples.Booking.JsonAntiCorruption;
using Ploeh.Samples.Booking.WebModel;

namespace Ploeh.Samples.Booking.Daemon
{
    class Program
    {
        static void Main()
        {
            RunUntilStopped(new QueueConsumer(Queue, JsonStreamObserver));
        }

        private static JsonStreamObserver JsonStreamObserver => new JsonStreamObserver(Quickenings, CompositeObserver);
        private static CompositeObserver<object> CompositeObserver => new CompositeObserver<object>(
            CommandDispatcher(CapacityGate), EventDispatcher(MonthViewUpdater));
        private static CommandDispatcher<T> CommandDispatcher<T>(ICommandHandler<T> commandHandler) => new CommandDispatcher<T>(commandHandler);
        private static CapacityGate CapacityGate => new CapacityGate(CapacityRepository, Channel<ReservationAcceptedEvent>(),
            Channel<ReservationRejectedEvent>(), Channel<SoldOutEvent>());
        private static IChannel<T> Channel<T>() where T : IMessage => new JsonChannel<T>(StoreWriter<T>());
        private static IStoreWriter<T> StoreWriter<T>() where T : IMessage => new FileQueueWriter<T>(QueueDirectory, Extension);
        private static EventDispatcher<T> EventDispatcher<T>(IEventHandler<T> eventHandler) => new EventDispatcher<T>(eventHandler);
        private static MonthViewUpdater MonthViewUpdater => new MonthViewUpdater(FileMonthViewStore);
        private static FileMonthViewStore FileMonthViewStore => new FileMonthViewStore(ViewStoreDirectory, Extension);
        private static ICapacityRepository CapacityRepository => new JsonCapacityRepository(FileDateStore, FileDateStore, Quickenings);
        private static IQueue Queue => new FileQueue(QueueDirectory, Extension);
        private static FileDateStore FileDateStore => new FileDateStore(SsotDirectory, Extension);
        private static DirectoryInfo SsotDirectory => new DirectoryInfo(@"..\..\..\BookingWebUI\SSoT").CreateIfAbsent();
        private static DirectoryInfo QueueDirectory => new DirectoryInfo(@"..\..\..\BookingWebUI\Queue").CreateIfAbsent();
        private static DirectoryInfo ViewStoreDirectory => new DirectoryInfo(@"..\..\..\BookingWebUI\ViewStore").CreateIfAbsent();
        private static string Extension => "txt";
        private static IQuickening[] Quickenings => new IQuickening[] {
            new CapacityReservedEvent.Quickening(),
            new RequestReservationCommand.Quickening(),
            new ReservationAcceptedEvent.Quickening(),
            new ReservationRejectedEvent.Quickening(),
            new SoldOutEvent.Quickening(),
        };

        #region Console stuff
        private static void RunUntilStopped(QueueConsumer q)
        {
            var tokenSource = new CancellationTokenSource();
            var token = tokenSource.Token;

            var task = Task.Factory.StartNew(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    q.ConsumeAll();
                    Thread.Sleep(500);
                }
            }, tokenSource.Token);

            Console.WriteLine("Type \"quit\" or \"exit\" to exit.");
            do
            {
                Console.Write("> ");
            } while (DoNotExit());

            tokenSource.Cancel();
        }

        private static bool DoNotExit()
        {
            var line = Console.ReadLine().ToUpperInvariant();
            return line != "QUIT"
                && line != "EXIT";
        }
        #endregion
    }
}