sudo hdfs dfs -get wasbs://scripts@$sparkBlobAccountName.blob.core.windows.net/msiserver.py /usr/hdinsight/msiserver.py
sudo hdfs dfs -get wasbs://scripts@$sparkBlobAccountName.blob.core.windows.net/msiserverapp.service /etc/systemd/system/msiserverapp.service
sudo chmod 644 /etc/systemd/system/msiserverapp.service
sudo systemctl daemon-reload
sudo systemctl enable msiserverapp.service
sudo systemctl start msiserverapp.service