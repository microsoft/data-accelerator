#!/bin/bash

echo "Remove the existing files if they exist"
sudo rm -f /usr/hdinsight/msiserver.py
sudo rm -f /etc/systemd/system/msiserverapp.service

echo "Download the files from HDFS/Blob storage"
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msiserver.py /usr/hdinsight/msiserver.py
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msiserverapp.service /etc/systemd/system/msiserverapp.service

echo "Change the permission of the file"
sudo chmod 644 /etc/systemd/system/msiserverapp.service

echo "Reload the systemd manager configuration to apply the changes"
sudo systemctl daemon-reload

echo "Enable ADAL service to start on boot"
sudo systemctl enable msiserverapp.service

if sudo systemctl is-active --quiet msalmsiserverapp.service; then
    echo "MSAL service is running, ending it and starting ADAL service"
    sudo systemctl stop msalmsiserverapp.service
    sudo systemctl start msiserverapp.service
elif sudo systemctl is-active --quiet msiserverapp.service; then
    echo "ADAL service is already running, restarting it"
    sudo systemctl restart msiserverapp.service
else
    echo "No service is running, starting ADAL service"
    sudo systemctl start msiserverapp.service
fi

echo "Script execution completed"