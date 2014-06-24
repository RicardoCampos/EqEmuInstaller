@echo off
 shared_memory.exe
 start loginserver.exe
 start world.exe
 echo waiting for the world to finish before starting zone...
 timeout /T 10 /NOBREAK
 start queryserv.exe
 start ucs.exe
 start eqlaunch.exe zone
 exit