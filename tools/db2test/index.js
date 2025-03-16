var ibmdb = require('ibm_db');

ibmdb.open("DRIVER={DB2};DATABASE=T2012;HOSTNAME=btoDevVm2012;UID=alta;PWD=Forget1;PORT=50000;PROTOCOL=TCPIP", 
  function (err,conn) {
    if (err) return console.log(err);

    conn.query('select * from usr order by userid', [10], function (err, data) {
      if (err) console.log(err);

      console.log(data);

      conn.close(function () {
        console.log('done');
      });
  });
});