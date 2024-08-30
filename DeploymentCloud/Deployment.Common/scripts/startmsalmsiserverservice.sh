#!/bin/bash

echo "Install Python Packages"
pip install msal

echo "Remove the existing files if they exist"
sudo rm -f /usr/hdinsight/msalmsiserver.py
sudo rm -f /etc/systemd/system/msalmsiserverapp.service

echo "Download the files from HDFS/Blob storage"
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msalmsiserver.py /usr/hdinsight/msalmsiserver.py
sudo hdfs dfs -copyToLocal wasbs://scriptactions@$sparkBlobAccountName.blob.core.windows.net/msalmsiserverapp.service /etc/systemd/system/msalmsiserverapp.service

echo "Change the permission of the file"
sudo chmod 644 /etc/systemd/system/msalmsiserverapp.service

echo "Reload the systemd manager configuration to apply the changes"
sudo systemctl daemon-reload

echo "Enable MSAL service to start on boot"
sudo systemctl enable msalmsiserverapp.service

if sudo systemctl is-active --quiet msiserverapp.service; then
    echo "ADAL service is running, ending it and starting MSAL service"
    sudo systemctl stop msiserverapp.service
    sudo systemctl start msalmsiserverapp.service
elif sudo systemctl is-active --quiet msalmsiserverapp.service; then
    echo "MSAL service is already running, restarting it"
    sudo systemctl restart msalmsiserverapp.service
else
    echo "No service is running, starting MSAL service"
    sudo systemctl start msalmsiserverapp.service
fi

echo "Script execution completed"