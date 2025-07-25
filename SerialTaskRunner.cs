using System;
using System.Threading;
using System.Threading.Tasks;
/*
//==================================
❗注意事項
只保護透過它執行的內容，如果你在外部修改共用變數，就還是會有 race condition。

RunTask() 是非同步的，呼叫時若沒 await 也可能造成順序失效（若你在 async void 中使用請小心）。
//==================================
example 1(async):
async Task incfuncAsync2()
{
    await incfunc();  
}
asyncWorker.RunTask(() => Console.WriteLine("Sync Task A"));
await asyncWorker.RunTask(incfuncAsync2);

//-------------------------
example 2.1(sync):
public void incfunc_sync()
{
    int x = vvv + 1;
    Thread.Sleep(100);            
    vvv = x;
    cnt++;
}
await asyncWorker.RunTask(incfunc_sync);
//-------------------------
example 2.2(sync):
Func<Task> innerfunction = () =>
{
    int x = vvv + 1;
    Thread.Sleep(100);
    vvv = x;
    cnt++;
    return Task.CompletedTask;  // 包裝成非同步格式
};

await asyncWorker2.RunTask(innerfunction);
//-------------------------
example 3(sync):

SerialTaskRunner asyncWorker2 = new SerialTaskRunner();
public async Task task1()
{
    for (int i = 0; i < 10; i++)
    {
        await asyncWorker2.RunTask(incfunc_sync);
        //incfunc_sync();
    }
}

public async Task task2()
{
    for (int i = 0; i < 10; i++)
        await asyncWorker2.RunTask(incfunc_sync);
    //incfunc_sync();
}
var t1 =  Task.Run(task1);
var t2 =  Task.Run(task2);
await t1;
await t2;
//==================================
*/
namespace ProfitWin
{
    public class SerialTaskRunner
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /// <summary>
        /// 執行非同步任務，並確保順序。
        /// </summary>
        public async Task RunTask(Func<Task> taskFunc)
        {
            if (taskFunc == null) throw new ArgumentNullException(nameof(taskFunc));

            await _semaphore.WaitAsync();
            try
            {
                await taskFunc();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AsyncSequentialWorker] Task failed: {ex}");
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <summary>
        /// 執行同步任務，並確保順序。
        /// </summary>
        public Task RunTask(Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));

            // 包成 Func<Task> 再呼叫上面的 RunTask
            return RunTask(() =>
            {
                action();
                return Task.CompletedTask;
            });
        }
    }
}
