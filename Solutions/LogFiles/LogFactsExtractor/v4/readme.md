
# Log Facts Extractor (V4)

Simple parser application which extracting from specified log file(or files) specified patterns
and save them in a local SQLite database file.

So, later you can use that SQLite db file to perform different kinds of analysis, build visualizations, etc.

# Some info

D:\JIRA\BTO-658-TcpGw-slow\4mc\
D:\JIRA\BTO-658-TcpGw-slow\logs\
D:\JIRA\BTO-658-TcpGw-slow\logs\test.1\
D:\JIRA\BTO-658-TcpGw-slow\logs\test.2\
D:\JIRA\BTO-658-TcpGw-slow\logs\cslmon\

	select * from CslmonFact where classId='krnsscr2' and txtValue1 like '%O:M#%'
	 order by appStart, logTime

	select * from CslmonFact where classId='krnsscr2' and factType in ('ExecSrvCall', 'ExecSrvCallDone')
	 order by appStart, logTime

	select * from CslmonFact where logTime between '2019-08-13:06:50:53.880000' and '2019-08-13:07:00:00.000000'
	 order by appStart, logTime

	select * from CslmonFact where factType='ExecCrash'
	 order by appStart, logTime

	INSERT INTO TcpGwFact (projectId, logId, lineNo, factType, line, logTime, tid, clientId, rep, msg, execTime, replySize, btoReply, responseMsg, created)\n                VALUES ( 3, 1, 52552, 'TcpGwCslWait', '20190813,065459.86 T13  [sync] cli#274856->Waiting for CSL...', '2019-08-13:06:54:59.860000', 'T13', 274856, null, null, null, null, null, null, '2019-08-28:18:58:03.156000' )

 

