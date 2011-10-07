﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ploeh.Samples.Booking.Persistence.FileSystem;
using System.IO;
using Ploeh.Samples.Booking.PersistenceModel;
using System.Threading;
using Ploeh.Samples.Booking.JsonAntiCorruption;
using Ploeh.Samples.Booking.DomainModel;
using System.Reactive.Subjects;
using System.Reactive.Disposables;

namespace Ploeh.Samples.Booking.Daemon
{
    class Program
    {
        static void Main(string[] args)
        {
            var queueDirectory = new DirectoryInfo(@"..\..\..\BookingWebUI\Queue");
            var queueMessageExtension = "txt";

            var disposable = new CompositeDisposable();
            var messageDispatcher = new Subject<object>();
            disposable.Add(
                messageDispatcher.Subscribe(
                    new Dispatcher<RequestReservationCommand>(
                        new CapacityGate(
                            new NullCapacityRepository(),
                            new JsonChannel<ReservationAcceptedEvent>(
                                new FileQueueWriter<ReservationAcceptedEvent>(
                                    queueDirectory,
                                    queueMessageExtension)),
                            new JsonChannel<ReservationRejectedEvent>(
                                new FileQueueWriter<ReservationRejectedEvent>(
                                    queueDirectory,
                                    queueMessageExtension)),
                            new JsonChannel<SoldOutEvent>(
                                new FileQueueWriter<SoldOutEvent>(
                                    queueDirectory,
                                    queueMessageExtension))))));

            var q = new QueueConsumer(
                new FileQueue(
                    queueDirectory,
                    queueMessageExtension),
                new JsonStreamObserver(
                    new[]
                    {
                        new RequestReservationCommand.Quickening()
                    },
                    messageDispatcher));

            while (true)
            {
                q.ConsumeAll();
                Thread.Sleep(500);
            }
        }

        private class NullCapacityRepository : ICapacityRepository
        {
            public IEnumerable<Capacity> Read(DateTime date)
            {
                return Enumerable.Empty<Capacity>();
            }

            public void Write(DateTime date, CapacityReservedEvent capacityReserved)
            {
            }
        }
    }
}
