#!/bin/bash

echo "Install Python Packages"
pip install msal

echo "Remove the existing files if they exist"
sudo rm -f /usr/hdinsight/msiserver.py
sudo rm -f /etc/systemd/system/msiserverapp.service

sudo rm -f /usr/hdinsight/msalmsiserver.py
sudo rm -f /etc/systemd/system/msalmsiserverapp.service

echo "Download the files from HDFS/Blob storage"
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msiserver.py /usr/hdinsight/msiserver.py
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msiserverapp.service /etc/systemd/system/msiserverapp.service

sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msalmsiserver.py /usr/hdinsight/msalmsiserver.py
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msalmsiserverapp.service /etc/systemd/system/msalmsiserverapp.service

echo "Change the permission of the file"
sudo chmod 644 /etc/systemd/system/msiserverapp.service
sudo chmod 644 /etc/systemd/system/msalmsiserverapp.service

echo "Reload the systemd manager configuration to apply the changes"
sudo systemctl daemon-reload

echo "Enable the service to start on boot"
sudo systemctl enable msiserverapp.service
sudo systemctl enable msalmsiserverapp.service

echo "Start the service"
sudo systemctl start msiserverapp.service
sudo systemctl start msalmsiserverapp.service

echo "Script execution completed"