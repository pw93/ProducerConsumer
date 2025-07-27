# -*- coding: utf-8 -*-


import stk_config as aicfg
import os
import threading
import sys
import datetime
from sty import fg, bg, rs
#import ai.color_text_console as color_console
import time

'''
Logger.clear_log_files()
logi('example info string','example info string2')
loge('example error string')
logv('example verbose string')
logw('example warning string')
Logger.set_display_level('error') #default: info. 'none', 'error', 'warning', 'info', 'verbose'
Logger.set_write_level('error') #default: verbose
Logger.is_buf_write = True #True/False, default: true

'''
import glob

logger_break_write=0
class Logger:

    _LEVEL_NONE=0
    _LEVEL_VERBOSE=1
    _LEVEL_INFO=2
    _LEVEL_WARNING=3
    _LEVEL_ERROR=4

    level_display=_LEVEL_INFO
    level_write=_LEVEL_VERBOSE

    dt_start = datetime.datetime.now()
    #dname_base example : '2021_11_14_010'
    
    dname_base = os.path.join(aicfg.path_db,'log',f'{dt_start.year}_{dt_start.month:02d}_{dt_start.day:02d}')
    

    dname_log_root = os.path.join(aicfg.path_db,'log')
    dname_exist=glob.glob(f'{dname_log_root}/*', recursive=False)

    #find the new directory base (if log in same day, cnt add one)
    cnt=1
    while True:
        dname_log = dname_base+f"_{cnt:03d}"
        is_found=False
        for d in dname_exist:
            if dname_log in d:
                is_found=True
                break
        if is_found:
            pass
        else:
            break
        cnt+=1


    fname_log = os.path.join(dname_log,'log_all.txt')
    fname_log_e = os.path.join(dname_log,'log_e.txt')
    fname_log_w = os.path.join(dname_log,'log_w.txt')

    del cnt
    del dname_log_root
    del dname_exist

    lock_log = threading.Lock() #one log should be processed first. Then process the next one.
    is_buf_write = True
    str_write=''
    str_write_e=''
    str_write_w=''
    lock_write = threading.Lock() #protect/lock str_print, str_write, str_write_e, str_write_w
    thread_write = None
    t_last=None #last time of write
    cmd_end=False
    postfix=''
    is_1st=True
    num_log=0
    num_loge=0
    num_logw=0

    st1 = '======================================================'
    st2 = f'===     now: [{dt_start.year}/{dt_start.month:02d}/{dt_start.day:02d} {dt_start.hour:02d}:{dt_start.minute:02d}:{dt_start.second:02d}] log start:   ==='
    st = f'\n{st1}\n{st2}\n{st1}'
    print(st)

    def end():
        Logger.cmd_end=True

    def set_dir_postfix(fix):
        Logger.postfix=fix

    def _create_dirs():        
        os.makedirs(Logger.dname_log,exist_ok=True)
        
        Logger.fname_log = os.path.join(Logger.dname_log,'log_all.txt')
        Logger.fname_log_e = os.path.join(Logger.dname_log,'log_e.txt')
        Logger.fname_log_w = os.path.join(Logger.dname_log,'log_w.txt')



    def write_thread_func():

        if Logger.postfix == '':
            pass
        else:
            Logger.dname_log+='_'+Logger.postfix

        if Logger.is_1st:
            Logger._create_dirs()
            Logger.fname_log = os.path.join(Logger.dname_log,'log_all.txt')
            Logger.fname_log_e = os.path.join(Logger.dname_log,'log_e.txt')
            Logger.fname_log_w = os.path.join(Logger.dname_log,'log_w.txt')

        Logger.is_1st=False


        Logger.t_last = datetime.datetime.now()

        cntt=0
        while True:



            #time.sleep(0.03)
            time.sleep(0.05)
            tid = threading.get_ident()
            #print(f'write {tid} {cntt}')
            cntt+=1

            Logger.lock_write.acquire()


            if len(Logger.str_write)>0 and len(Logger.fname_log)>0:
                f = open(Logger.fname_log,'a',encoding='utf8')
                f.write(Logger.str_write)
                f.close()
                Logger.str_write=''
                Logger.t_last = datetime.datetime.now()

            if len(Logger.str_write_e)>0 and len(Logger.fname_log_e)>0:
                f = open(Logger.fname_log_e,'a',encoding='utf8')
                f.write(Logger.str_write_e)
                f.close()
                Logger.str_write_e=''
                Logger.t_last = datetime.datetime.now()

            if len(Logger.str_write_w)>0 and len(Logger.fname_log_w)>0:
                f = open(Logger.fname_log_w,'a',encoding='utf8')
                f.write(Logger.str_write_w)
                f.close()
                Logger.str_write_w=''
                Logger.t_last = datetime.datetime.now()

            tnow = datetime.datetime.now()

            Logger.lock_write.release()

            if (tnow-Logger.t_last).total_seconds() > 0.5 : #3 second
                break

            if Logger.cmd_end:
                print('cmd_end, break write_while')
                break

            if os.getenv('LOGGER_WRITE_THREAD_BREAK')=='1':
                print('logger_break_write, break write_while')
                break


        print('write_thread exit')
        Logger.lock_write.acquire()
        Logger.thread_write=None
        Logger.lock_write.release()




    def set_display_level(level):
        Logger.level_display=Logger._LEVEL_VERBOSE
        if level=='none':
            Logger.level_display=Logger._LEVEL_NONE
        if level=='verbose':
            Logger.level_display=Logger._LEVEL_VERBOSE
        if level=='info':
            Logger.level_display=Logger._LEVEL_INFO
        if level=='warning':
            Logger.level_display=Logger._LEVEL_WARNING
        if level=='error':
            Logger.level_display=Logger._LEVEL_ERROR

    def set_write_level(level):
        Logger.levle_write=Logger._LEVEL_VERBOSE
        if level=='none':
            Logger.levle_write=Logger._LEVEL_NONE
        if level=='verbose':
            Logger.levle_write=Logger._LEVEL_VERBOSE
        if level=='info':
            Logger.levle_write=Logger._LEVEL_INFO
        if level=='warning':
            Logger.levle_write=Logger._LEVEL_WARNING
        if level=='error':
            Logger.levle_write=Logger._LEVEL_ERROR



    def get_dt_str():
        dt = datetime.datetime.now()
        slog = f'{dt.hour:02d}:{dt.minute:02d}:{dt.second:02d}.{dt.microsecond//1000:03d}'
        return slog

    def get_log_str(*vartuple, level, sep=' ', end='\n', divide=False):
        str_dt = Logger.get_dt_str()
        tid = threading.get_ident()
        list_string = [str(w) for w in vartuple]
        ss=sep.join(list_string)

        if divide and ss.startswith('['):
            list_string = [str(w) for w in vartuple]
            ss=sep.join(list_string[1:])
            ssa = f'[{str_dt} tid:{tid} {level}]{list_string[0]}'
            ssb = f'{ss}'
            ss2 = [ssa, ssb]
        elif divide:
            ssa = f'[{str_dt} tid:{tid} {level}]'
            ssb = f'{ss}'
            ss2 = [ssa, ssb]
        elif ss.startswith('['):
            ss2 = f'[{str_dt} tid:{tid} {level}]{ss}'
        else:
            ss2 = f'[{str_dt} tid:{tid} {level}] {ss}'
        return ss2

    def clear_log_files():
        if os.path.exists(Logger.fname_log_w):
            os.remove(Logger.fname_log_w)
        if os.path.exists(Logger.fname_log_e):
            os.remove(Logger.fname_log_e)
        if os.path.exists(Logger.fname_log):
            os.remove(Logger.fname_log)

    def write_log(s, level=_LEVEL_NONE, end='\n'):
        if not Logger.is_buf_write:
            Logger.lock_write.acquire()
            ss=s+'\n'
            if level != Logger._LEVEL_NONE and len(Logger.fname_log)>0:
                f = open(Logger.fname_log,'a')
                f.write(ss)
                f.close()
            if level==Logger._LEVEL_ERROR and len(Logger.fname_log_e)>0:
                f = open(Logger.fname_log_e,'a')
                f.write(ss)
                f.close()
            if level==Logger._LEVEL_WARNING and len(Logger.fname_log_w)>0:
                f = open(Logger.fname_log_w,'a')
                f.write(ss)
                f.close()
            Logger.lock_write.release()
        else:
            Logger.lock_write.acquire()
            if Logger.thread_write==None:
                Logger.thread_write = threading.Thread(target=Logger.write_thread_func)
                Logger.thread_write.start()


            ss=s+'\n'

            if level != Logger._LEVEL_NONE and len(Logger.fname_log)>0:
                Logger.str_write+=ss
            if level==Logger._LEVEL_ERROR and len(Logger.fname_log_e)>0:
                Logger.str_write_e+=ss
            if level==Logger._LEVEL_WARNING and len(Logger.fname_log_w)>0:
                Logger.str_write_w+=ss

            Logger.lock_write.release()

    def log_sum():
        t = datetime.datetime.now()
        s2='\n==================\n|| log sumary:\n'
        s3=f'|| total log: {Logger.num_log}\n'
        s4=f'|| total logw: {Logger.num_logw}\n'
        s5=f'|| total loge: {Logger.num_loge}\n'
        s6=f'|| total time: {t-Logger.dt_start}\n'
        s7='=================='
        s=s2+s3+s4+s5+s6+s7
        logi(s)



    def log_e(*vartuple, sep=' ', end='\n'):
        Logger.num_log+=1
        Logger.num_loge+=1
        ss = Logger.get_log_str(*vartuple, level='Error', sep=sep, end=end)
        Logger.lock_log.acquire()
        if Logger.level_display in [Logger._LEVEL_ERROR,Logger._LEVEL_WARNING,Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.style_e()
            print(ss,sep=sep,end=end)
            Logger.reset_style()
        Logger.lock_log.release()



        if Logger.level_write in [Logger._LEVEL_ERROR,Logger._LEVEL_WARNING,Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.write_log(ss,level=Logger._LEVEL_ERROR,end=end)





    def log_w(*vartuple, sep=' ', end='\n'):
        Logger.num_log+=1
        Logger.num_logw+=1
        ss = Logger.get_log_str(*vartuple, level='Warning', sep=sep, end=end)
        Logger.lock_log.acquire()
        if Logger.level_display in [Logger._LEVEL_WARNING,Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.style_w()
            print(ss,sep=sep,end=end)
            Logger.reset_style()
        Logger.lock_log.release()



        if Logger.level_write in [Logger._LEVEL_WARNING,Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.write_log(ss,level=Logger._LEVEL_WARNING,end=end)

    def log_i2(*vartuple, sep=' ', end='\n', color=None, color2=None):
        Logger.num_log+=1
        ss2 = Logger.get_log_str(*vartuple, level='Info2', sep=sep, end=end, divide=True)
        ss=ss2[0]+ss2[1]

        if color==None:
            pass
        elif type(color)==str:
            if color=='blue':
                color_b=[0,0,255]
            elif color=='red':
                color_b=[255,0,0]
            elif color=='green':
                color_b=[0,255,0]
            elif color=='yellow':
                color_b=[255,255,0]
            elif color=='light blue':
                color_b=[128,200,255]
            elif color=='light red':
                color_b=[255,128,128]
            elif color=='light green':
                color_b=[200,255,200]
        else:
            color_b=color

        if color2==None:
            pass
        elif type(color2)==str:
            if color2=='blue':
                color_a=[0,0,255]
            elif color2=='red':
                color_a=[255,0,0]
            elif color2=='green':
                color_a=[0,255,0]
            elif color2=='yellow':
                color_a=[255,255,0]
            elif color2=='light blue':
                color_a=[128,200,255]
            elif color2=='light red':
                color_a=[255,128,128]
            elif color2=='light green':
                color_a=[200,255,200]
        else:
            color_a=color2


        Logger.lock_log.acquire()
        if Logger.level_display in [Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            if color2 != None:
                Logger.style_i_clr([0,0,0], color_a)
            print(ss2[0],sep=sep,end='')

            if color != None:
                Logger.style_i_clr([0,0,0], color_b)
            print(ss2[1],sep=sep,end=end)
            Logger.reset_style()
        Logger.lock_log.release()

        if Logger.level_write in [Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.write_log(ss,level=Logger._LEVEL_INFO,end=end)


    def log_i(*vartuple, sep=' ', end='\n', color=None):
        Logger.num_log+=1
        ss = Logger.get_log_str(*vartuple, level='Info', sep=sep, end=end)

        Logger.lock_log.acquire()
        if Logger.level_display in [Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.style_i()
            print(ss,sep=sep,end=end)
            Logger.reset_style()

        Logger.lock_log.release()



        if Logger.level_write in [Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.write_log(ss,level=Logger._LEVEL_INFO,end=end)



    def log_i_clr(*vartuple, sep=' ', end='\n', clr_fg, clr_bg):
        ss = Logger.get_log_str(*vartuple, level='Info', sep=sep, end=end)
        Logger.lock_log.acquire()
        if Logger.level_display in [Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.style_i_clr(clr_fg,clr_bg)
            print(ss,sep=sep,end=end)
            Logger.reset_style()
        Logger.lock_log.release()



        if Logger.level_write in [Logger._LEVEL_INFO,Logger._LEVEL_VERBOSE]:
            Logger.write_log(ss,level=Logger._LEVEL_INFO,end=end)



    def log_v(*vartuple, sep=' ', end='\n'):
        Logger.num_log+=1
        ss = Logger.get_log_str(*vartuple, level='Verbose', sep=sep, end=end)
        Logger.lock_log.acquire()
        if Logger.level_display in [Logger._LEVEL_VERBOSE]:
            Logger.style_i()
            print(ss,sep=sep,end=end)
            Logger.reset_style()
        Logger.lock_log.release()


        if Logger.level_write in [Logger._LEVEL_VERBOSE]:
            Logger.write_log(ss,level=Logger._LEVEL_VERBOSE,end=end)





    def style_i():
        pass

    def style_v():
        pass


    def style_i_clr(clr_fg, clr_bg):

        print(fg(clr_fg[0],clr_fg[1],clr_fg[2]),end='')
        print(bg(clr_bg[0],clr_bg[1],clr_bg[2]),end='')


    def style_w():

        print(fg(0,0,0),end='')
        print(bg(255,230,180),end='')




    def style_e():

        #print(fg(255,0,0),end='')
        #print(bg(100,255,150),end='')

        print(fg(0,0,0),end='')
        print(bg(255,100,100),end='')

        '''

        Util.cmdcolor_fg('red')
        Util.cmdcolor_bg('green_light')
        Util.sty_fg(255,0,0)
        Util.sty_bg(100,255,150)
        '''

    def reset_style():

        print(rs.all,end='')
        '''
        Util.sty_clear_all()
        Util.cmdcolor_clear()
        '''




import traceback
def get_exception_detail(e):
    #    print(e)
    error_class = e.__class__.__name__ #取得錯誤類型
    detail = e.args[0] #取得詳細內容
    cl, exc, tb = sys.exc_info() #取得Call Stack

    errMsg = '\nTraceback:\n'
    errMsg += f'error class: {error_class}\n'
    errMsg += f'detail: {detail}\n'

    sz = len(traceback.extract_tb(tb))
    for i in range(sz):
        lastCallStack = traceback.extract_tb(tb)[i] #取得Call Stack的最後一筆資料
        fileName = lastCallStack[0] #取得發生的檔案名稱
        lineNum = lastCallStack[1] #取得發生的行號
        funcName = lastCallStack[2] #取得發生的函數名稱
        content = lastCallStack[3]
        #errMsg = "File \"{}\", line {}, in {}: [{}] {}".format(fileName, lineNum, funcName, error_class, detail)
        errMsg += f'#{i} (total:{sz})\n'
        errMsg += f'file: {fileName}\n'
        errMsg += f'line: {lineNum}\n'
        errMsg += f'function: {funcName}\n'
        errMsg += f'content: {content}\n'

    return errMsg

import os
import sys
from inspect import currentframe, getframeinfo
def logi(*vartuple, sep=' ', end='\n'):
    #cf = currentframe()
    cf = sys._getframe(1)
    fno = cf.f_lineno
    fname = getframeinfo(cf).filename
    fname = os.path.basename(fname)
    lst=[f'[{fname}:{fno}]']+list(vartuple)
    tuple2 = tuple(lst)
    Logger.log_i(*tuple2, sep=sep, end=end)


#color: bg color of debug string, color2: bg color of helper-info
def logi2(*vartuple, sep=' ', end='\n', color=None, color2=None):
    #cf = currentframe()
    cf = sys._getframe(1)
    fno = cf.f_lineno
    fname = getframeinfo(cf).filename
    fname = os.path.basename(fname)
    lst=[f'[{fname}:{fno}]']+list(vartuple)
    tuple2 = tuple(lst)
    if color==None:
        Logger.log_i2(*tuple2, sep=sep, end=end, color='light blue', color2=color2)
    else:
        Logger.log_i2(*tuple2, sep=sep, end=end, color=color, color2=color2)


def loge(*vartuple, sep=' ', end='\n'):
    cf = sys._getframe(1)
    fno = cf.f_lineno
    fname = getframeinfo(cf).filename
    fname = os.path.basename(fname)
    lst=[f'[{fname}:{fno}]']+list(vartuple)
    tuple2 = tuple(lst)
    Logger.log_e(*tuple2, sep=sep, end=end)

def logw(*vartuple, sep=' ', end='\n'):
    cf = sys._getframe(1)
    fno = cf.f_lineno
    fname = getframeinfo(cf).filename
    fname = os.path.basename(fname)
    lst=[f'[{fname}:{fno}]']+list(vartuple)
    tuple2 = tuple(lst)
    Logger.log_w(*tuple2, sep=sep, end=end)


def logv(*vartuple, sep=' ', end='\n'):
    cf = sys._getframe(1)
    fno = cf.f_lineno
    fname = getframeinfo(cf).filename
    fname = os.path.basename(fname)
    lst=[f'[{fname}:{fno}]']+list(vartuple)
    tuple2 = tuple(lst)
    Logger.log_v(*tuple2, sep=sep, end=end)

def log_sum():
    Logger.log_sum()

def log_end():
    Logger.end()


def _test1():
    #Logger.level_display='error'
    #Logger.levle_write='verbose'
    Logger.clear_log_files()
    loge('aa','bb')
    a=1/0

    loge('aa','bb')
    print('sdf')
    logw('aa','bb2')
    logw('aa','bb2')
    logi('aa','bb2')
    logv('aa','bb2')


def _test2():
    try:
        _test1()#adf
    except Exception as e:
        ss=get_exception_detail(e)
        loge(ss)
        loge("Exception:",get_exception_detail(e))

import os
import sys
import time

if __name__ == '__main__':

    #os.system('taskkill /F /PID python.exe')
    #taskkill /F /PID pid_number
    #_test2()
    ss="test"


    #Logger.set_dir_postfix('q')

    loge(ss)
    loge('ss')
    logv('ssv')
    logw('ssv')
    log_sum()
    tid = threading.get_ident()

    #print(tid,b)
    time.sleep(1)
    logw('ssv')
    logw('ssv')

    #Logger.cmd_end = True
    #log_end()

    a=3
    del a
