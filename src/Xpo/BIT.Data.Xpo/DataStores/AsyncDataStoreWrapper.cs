﻿
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using DevExpress.Xpo.DB;
using DevExpress.Xpo.DB.Helpers;
using DevExpress.Xpo.Helpers;
using DevExpress.Data.Helpers;
using System.Threading.Tasks;

namespace BIT.Xpo.DataStores
{
    public class AsyncDataStoreWrapper : IDataStore, IDataStoreAsync, IDataStoreSchemaExplorer, ICommandChannel, ICommandChannelAsync
    {
        readonly IDataStore DataStore;
        public AsyncDataStoreWrapper() { }
        public AsyncDataStoreWrapper(IDataStore dataStore)
            : this()
        {
            this.DataStore = dataStore;
        }
        static bool IsStaCurrentThread
        {
            get { return Thread.CurrentThread.GetApartmentState() == ApartmentState.STA; }
        }
        #region IDataStore Members
        public AutoCreateOption AutoCreateOption
        {
            get
            {
                return StaSafeHelper.Invoke(() => DataStore.AutoCreateOption);
            }
        }
        public ModificationResult ModifyData(params ModificationStatement[] dmlStatements)
        {
            return StaSafeHelper.Invoke(() => DataStore.ModifyData(dmlStatements));
        }
        public SelectedData SelectData(params SelectStatement[] selects)
        {
            return StaSafeHelper.Invoke(() => DataStore.SelectData(selects));
        }
        public UpdateSchemaResult UpdateSchema(bool doNotCreateIfFirstTableNotExist, params DBTable[] tables)
        {
            return StaSafeHelper.Invoke(() => DataStore.UpdateSchema(doNotCreateIfFirstTableNotExist, tables));
        }
        #endregion
        #region IDataStoreAsync Members
        public Task<SelectedData> SelectDataAsync(CancellationToken cancellationToken, params SelectStatement[] selects)
        {
            var dataStoreAsync = DataStore as IDataStoreAsync;
            if (dataStoreAsync == null || IsStaCurrentThread)
            {
                return Task.Factory.StartNew(() => DataStore.SelectData(selects), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            return dataStoreAsync.SelectDataAsync(cancellationToken, selects);
        }
        public Task<ModificationResult> ModifyDataAsync(CancellationToken cancellationToken, params ModificationStatement[] dmlStatements)
        {
            var dataStoreAsync = DataStore as IDataStoreAsync;
            if (dataStoreAsync == null || IsStaCurrentThread)
            {
                return Task.Factory.StartNew(() => DataStore.ModifyData(dmlStatements), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            return dataStoreAsync.ModifyDataAsync(cancellationToken, dmlStatements);
        }

        //TODO Implement https://supportcenter.devexpress.com/ticket/details/t917965/how-should-i-implement-updateschemaasync-and-createobjecttypeasync-for-the-interface
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<UpdateSchemaResult> UpdateSchemaAsync(CancellationToken cancellationToken, bool doNotCreateIfFirstTableNotExist, params DBTable[] tables)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            //IDataStore provider = await AcquireUpdateSchemaProviderAsync(cancellationToken).ConfigureAwait(false);
            //var providerAsync = provider as IDataStoreAsync;
            //try
            //{
            //    if (providerAsync == null)
            //    {
            //        throw new InvalidOperationException(DbRes.GetString(DbRes.Async_ConnectionProviderDoesNotImplementIDataStoreAsync, (provider != null) ? provider.GetType().FullName : ""));
            //    }
            //    return await providerAsync.UpdateSchemaAsync(cancellationToken, doNotCreateIfFirstTableNotExist, tables).ConfigureAwait(false);
            //}
            //finally
            //{
            //    ReleaseUpdateSchemaProvider(provider);
            //}
            throw new NotImplementedException();
        }

        #endregion
        #region IDataStoreSchemaExplorer Members
        public string[] GetStorageTablesList(bool includeViews)
        {
            return StaSafeHelper.Invoke(() => ((IDataStoreSchemaExplorer)DataStore).GetStorageTablesList(includeViews));
        }
        public DBTable[] GetStorageTables(params string[] tables)
        {
            return StaSafeHelper.Invoke(() => ((IDataStoreSchemaExplorer)DataStore).GetStorageTables(tables));
        }
        #endregion
        #region ICommandChannel / ICommandChannelAsync Members
        public object Do(string command, object args)
        {
            ICommandChannel nestedCommandChannel = DataStore as ICommandChannel;
            if (nestedCommandChannel == null)
            {
                if (DataStore == null) throw new NotSupportedException(string.Format(CommandChannelHelper.Message_CommandIsNotSupported, command));
                else throw new NotSupportedException(string.Format(CommandChannelHelper.Message_CommandIsNotSupportedEx, command, DataStore.GetType()));
            }
            return StaSafeHelper.Invoke(() => nestedCommandChannel.Do(command, args));
        }
        public Task<object> DoAsync(string command, object args, CancellationToken cancellationToken = default(CancellationToken))
        {
            ICommandChannelAsync nestedCommandChannelAsync = DataStore as ICommandChannelAsync;
            if (nestedCommandChannelAsync == null || IsStaCurrentThread)
            {
                ICommandChannel nestedCommandChannel = DataStore as ICommandChannel;
                if (nestedCommandChannel == null)
                {
                    if (DataStore == null) throw new NotSupportedException(string.Format(CommandChannelHelper.Message_CommandIsNotSupported, command));
                    else throw new NotSupportedException(string.Format(CommandChannelHelper.Message_CommandIsNotSupportedEx, command, DataStore.GetType()));
                }
                return Task.Factory.StartNew(() => nestedCommandChannel.Do(command, args), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
            return nestedCommandChannelAsync.DoAsync(command, args, cancellationToken);
        }
        #endregion
    }
}
