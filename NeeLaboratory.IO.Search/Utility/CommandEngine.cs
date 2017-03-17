// Copyright (c) 2016 Mitsuhiro Ito (nee)
//
// This software is released under the MIT License.
// http://opensource.org/licenses/mit-license.php

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NeeLaboratory.IO.Utility
{
    /// <summary>
    /// コマンドインターフェイス
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// コマンドキャンセル
        /// </summary>
        //void Cancel();

        /// <summary>
        /// コマンド実行
        /// </summary>
        /// <returns></returns>
        Task ExecuteAsync();
    }


    /// <summary>
    /// コマンド実行結果
    /// </summary>
    public enum CommandResult
    {
        None,
        Completed,
        Canceled,
    }

    /// <summary>
    /// コマンド基底
    /// キャンセル、終了待機対応
    /// </summary>
    public abstract class CommandBase : ICommand
    {
        // キャンセルトークン
        private CancellationToken _cancellationToken;
        public CancellationToken CancellationToken
        {
            get { return _cancellationToken; }
            set { _cancellationToken = value; }
        }

        // コマンド終了通知
        private ManualResetEventSlim _complete = new ManualResetEventSlim(false);

        // コマンド実行結果
        private CommandResult _result;
        public CommandResult Result
        {
            get { return _result; }
            set { _result = value; _complete.Set(); }
        }

        // キャンセル可能フラグ
        public bool CanBeCanceled => _cancellationToken.CanBeCanceled;

        /// <summary>
        /// constructor
        /// </summary>
        public CommandBase()
        {
            _cancellationToken = CancellationToken.None;
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="token"></param>
        public CommandBase(CancellationToken token)
        {
            _cancellationToken = token;
        }


        /// <summary>
        /// コマンド実行
        /// </summary>
        /// <returns></returns>
        public async Task ExecuteAsync()
        {
            if (_complete.IsSet) return;

            // cancel ?
            if (_cancellationToken.IsCancellationRequested)
            {
                Result = CommandResult.Canceled;
                return;
            }

            // execute
            try
            {
                await ExecuteAsync(_cancellationToken);
                Result = CommandResult.Completed;
            }
            catch (OperationCanceledException)
            {
                Result = CommandResult.Canceled;
                OnCanceled();
            }
            catch (Exception e)
            {
                OnException(e);
                throw;
            }
        }

        /// <summary>
        /// コマンド終了待機
        /// </summary>
        /// <returns></returns>
        public async Task WaitAsync()
        {
            await Task.Run(() => _complete.Wait());
        }

        static int _serial;

        /// <summary>
        /// コマンド終了待機
        /// </summary>
        /// <returns></returns>
        public async Task WaitAsync(CancellationToken token)
        {
            var serial = _serial++;

            var sw = Stopwatch.StartNew();
            await Task.Run(async () =>
            {
                await Task.Yield();
                _complete.Wait(token);
                Debug.WriteLine($"{serial}: WaitTask done.");
            });
            Debug.WriteLine($"{serial}: WaitTime = {sw.ElapsedMilliseconds}ms");
        }


        /// <summary>
        /// コマンド実行(abstract)
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        protected abstract Task ExecuteAsync(CancellationToken token);


        /// <summary>
        /// コマンドキャンセル時
        /// </summary>
        protected virtual void OnCanceled()
        {
        }

        /// <summary>
        /// コマンド例外時
        /// </summary>
        /// <param name="e"></param>
        protected virtual void OnException(Exception e)
        {
        }

    }



    /// <summary>
    /// コマンドエンジン
    /// </summary>
    public class CommandEngine : IDisposable
    {
        // ワーカータスクのキャンセルトークン
        private CancellationTokenSource _cancellationTokenSource;

        // 予約コマンド存在通知
        private ManualResetEventSlim _ready = new ManualResetEventSlim(false);

        // 排他処理用ロックオブジェクト
        private object _lock = new object();

        // コマンドリスト
        protected Queue<ICommand> _queue = new Queue<ICommand>();

        // 実行中コマンド
        protected ICommand _command;

        /// <summary>
        /// コマンド登録
        /// </summary>
        /// <param name="command"></param>
        public virtual void Enqueue(ICommand command)
        {
            lock (_lock)
            {
                if (OnEnqueueing(command))
                {
                    _queue.Enqueue(command);
                    OnEnqueued(command);
                    _ready.Set();
                }
            }
        }

        /// <summary>
        /// Queue登録前の処理
        /// </summary>
        /// <param name="command"></param>
        protected virtual bool OnEnqueueing(ICommand command)
        {
            return true;
        }

        /// <summary>
        /// Queue登録後の処理
        /// </summary>
        protected virtual void OnEnqueued(ICommand command)
        {
            // nop.
        }

        /// <summary>
        /// 現在のコマンド数
        /// </summary>
        public int Count
        {
            get { return _queue.Count + (_command != null ? 1 : 0); }
        }


        /// <summary>
        /// 初期化
        /// ワーカータスク起動
        /// </summary>
        public void Initialize()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(async () =>
            {
                // TODO: ここでの例外が補足できていない。致命的！
                // システム停止レベル！
                try
                {
                    await WorkerAsync(_cancellationTokenSource.Token);
                }
                catch (Exception e)
                {
                    Debug.WriteLine($"!!!! EXCEPTION !!!!: {e.Message}\n{e.StackTrace}");
                    Debugger.Break();
                }
            });
        }

        /// <summary>
        /// ワーカータスク終了
        /// </summary>
        public virtual void Dispose()
        {
            lock (_lock)
            {
                _cancellationTokenSource?.Cancel();
                //_command?.Cancel();
            }
        }

        /// <summary>
        /// ワーカータスク
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private async Task WorkerAsync(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    _ready.Wait(token);

                    while (!token.IsCancellationRequested)
                    {
                        lock (_lock)
                        {
                            if (_queue.Count <= 0)
                            {
                                _command = null;
                                _ready.Reset();
                                break;
                            }

                            _command = _queue.Dequeue();
                        }

                        Debug.WriteLine($"{_command}: rest={_queue.Count}");
                        await _command?.ExecuteAsync();
                        Debug.WriteLine($"{_command} done.");

                        _command = null;
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _command = null;
            }
        }
    }

}
