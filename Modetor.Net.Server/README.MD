**Modetor.Net.Server**

This **.Net Core 3.1** Project aims to provide a customizable, more secure and fast executions.

With help of it's built-in ``runtime-reconfig`` made it more brilliant.


for example, you can customize:


**Main Settings **


**main-page**: root/home.html ``; main page. it can be python source code``

**fnf-page**: root/404.html ``; when requesting an unfounded file. it can be python source code``

**allow-smart-switch**: true/false ``; change server's address based on Network changes``

**allow-network-connections**: true/false ``; whether devices in same network can reach the server or not``

**max-clients-count**: 0 or [1,2,3...] ``; 0 means infinity``

**repos** = MyApp, MyUnPulishedApp,... ``; these're the websites(basically they're folders) other devices can request. other folders cannot be accessed direclty``

**connections-handler**: root/MyScript.py ``; handling connections in python``

**connections-handler-repos**: MyApp, MyUnPulishedApp,.. or * ``; requests that belongs to these repositories be handling connections in python. * means all repos``



**Threading settings **

**thread-mechanism**: 0,1,2 ``; 0 means signle thread, 1 means multithread and 2 means multithread with queues (called Active Pips)``

<i>pips stay in unactivated while there's no work to do</i>

**pips-count**: 1,2,3... cannot be less than 1 ``; pips are threads that stay alive to process the work, more pips better parallel performance``

**queue-capacity**: 1,2,3... ``; how much processes does queue can contain before switch thread's state to active`` [BETA]

**communication-mechanism**: async, sync``; All Network IO Operations runs either async or sync``


