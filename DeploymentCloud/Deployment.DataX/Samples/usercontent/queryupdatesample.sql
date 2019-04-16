--DataXStates--
CREATE TABLE iotsample_GarageDoor_status_accumulated
    (deviceId long, deviceType string, homeId long, EventTime Timestamp, Reading long);

--DataXQuery--
DeviceWindowedInput = SELECT 
                        deviceDetails.deviceId,
                        deviceDetails.deviceType,
                        eventTimeStamp,
                        deviceDetails.homeId,
                        deviceDetails.status
                    FROM DataXProcessedInput 
                    TIMEWINDOW('5 minutes')
                   GROUP BY deviceId, deviceType, eventTimeStamp, homeId, status;

--DataXQuery--
DeviceNotWindowedInputWithName = SELECT 
                        DataXProcessedInput.deviceDetails.deviceId,
                        DataXProcessedInput.deviceDetails.deviceType,
                        eventTimeStamp,
                        DataXProcessedInput.deviceDetails.homeId,
                        DataXProcessedInput.deviceDetails.status,
                        myDevicesRefdata.deviceName
                    FROM DataXProcessedInput 
                    JOIN myDevicesRefdata 
                    ON DataXProcessedInput.deviceDetails.deviceId = myDevicesRefdata.deviceId 
                    AND DataXProcessedInput.deviceDetails.homeId = myDevicesRefdata.homeId;

--DataXQuery--
DeviceNotWindowedInputWithNameAndWho = SELECT 
                        deviceId,
                        deviceType,
                        eventTimeStamp,
                        homeId,
                        status,
                        deviceName,
                        whoOpened(CAST(deviceId AS String)) AS whoOpened
                    FROM DeviceNotWindowedInputWithName; 

--DataXQuery--
DeviceInfoTimeWindow = SELECT 
                    deviceId,
                    deviceType,
                    homeId,
                    MAX(eventTimeStamp) AS MaxEventTime,
                    MIN(status) AS MinReading,
                    MAX(status) AS MaxReading
                FROM DeviceWindowedInput
                GROUP BY deviceId, deviceType, homeId;

--DataXQuery--
HeaterStateFiltered = SELECT 
                        eventTimeStamp,
                        deviceDetails.deviceId,
                        deviceDetails.deviceType,
                        deviceDetails.homeId,
                        deviceDetails.status
					FROM DataXProcessedInput
					WHERE deviceDetails.homeId = 150
						AND deviceDetails.deviceType = 'Heating'
					GROUP BY eventTimeStamp, deviceId, deviceType, homeId, status;

--DataXQuery--
HeaterStateOneIsOn = CreateMetric(HeaterStateFiltered, status);

--DataXQuery--
WindowLockStateFiltered = SELECT 
                                eventTimeStamp,
                                deviceDetails.deviceId,
                                deviceDetails.deviceType,
                                deviceDetails.homeId,
                                deviceDetails.status
							FROM DataXProcessedInput
							WHERE deviceDetails.homeId = 150
								AND deviceDetails.deviceType = 'WindowLock'
							GROUP BY eventTimeStamp, deviceId, deviceType, homeId, status;
                    
--DataXQuery--
WindowLockStateOneIsLocked =  CreateMetric(WindowLockStateFiltered, status);

--DataXQuery--
WindowLockWindowedFiltered = SELECT *
							FROM DeviceInfoTimeWindow
							INNER JOIN WindowLockStateFiltered ON WindowLockStateFiltered.eventTimeStamp = DeviceInfoTimeWindow.MaxEventTime
							WHERE DeviceInfoTimeWindow.homeId = 150
								AND DeviceInfoTimeWindow.deviceType = 'WindowLock';

--DataXQuery--
WindowLockStateWindowed = CreateMetric(WindowLockWindowedFiltered, MaxReading);

--DataXQuery--
WindowOpenFiveMin = SELECT
                        MaxEventTime,
                        MaxReading
                    FROM DeviceInfoTimeWindow
                    INNER JOIN WindowLockStateFiltered ON WindowLockStateFiltered.eventTimeStamp = DeviceInfoTimeWindow.MaxEventTime
                    WHERE DeviceInfoTimeWindow.homeId = 150
                        AND DeviceInfoTimeWindow.MaxReading = 0
                        AND DeviceInfoTimeWindow.deviceType = 'WindowLock';
						
--DataXQuery--
WindowOpenFiveMinWhileHeaterOnCombinedAlert = SELECT
                                    MaxEventTime AS EventTime,
                                    'WindowOpenFiveMinWhileHeaterOnCombinedAlert' AS MetricName,
                                    0 AS Metric,
                                    'iotsample' AS Product, 
                                    'Window open for 5+ minutes while heater is on.' AS Pivot1
                                FROM WindowOpenFiveMin
                                INNER JOIN HeaterStateFiltered ON HeaterStateFiltered.eventTimeStamp = WindowOpenFiveMin.MaxEventTime
                                WHERE WindowOpenFiveMin.MaxReading = 0
                                    AND HeaterStateFiltered.status = 1;

--DataXQuery--
DoorLockStatusFiltered = SELECT *
						FROM DataXProcessedInput
						WHERE deviceDetails.homeId = 150                  
							AND deviceDetails.deviceType = 'DoorLock';

--DataXQuery--
DoorLockStatusOneForLocked =  CreateMetric(DoorLockStatusFiltered, deviceDetails.status);

--DataXQuery--
DoorUnlockedSimpleAlert = SELECT DISTINCT
                        eventTimeStamp AS EventTime,
                        'DoorUnlockedSimpleAlert' AS MetricName,
                        0 AS Metric,
                        'iotsample' AS Product, 
                        CONCAT('Door unlocked: ', deviceName, ' at home ', homeId) AS Pivot1
                        FROM DeviceNotWindowedInputWithNameAndWho
                        WHERE deviceType = 'DoorLock' AND
                        homeId = 150 AND
                        status = 0;

--DataXQuery--
GarageDoorStatusFiltered = SELECT 
							eventTimeStamp,
							deviceDetails.deviceId,
							deviceDetails.deviceType,
							deviceDetails.homeId,
							deviceDetails.status
						FROM DataXProcessedInput
						WHERE deviceDetails.homeId = 150
							AND deviceDetails.deviceType = 'GarageDoorLock'
						GROUP BY eventTimeStamp, deviceId, deviceType, homeId, status;

--DataXQuery--
GarageDoorStatusOneForLocked = CreateMetric(GarageDoorStatusFiltered, status);

--DataXQuery--
GarageDoorAccumalator = SELECT 
                            deviceId,
                            deviceType,
                            homeId,
                            eventTimeStamp AS EventTime,
                            status AS Reading
                        FROM DeviceNotWindowedInputWithName
                        WHERE homeId = 150
                            AND deviceType = 'GarageDoorLock'
                        UNION ALL
                        SELECT 
                            deviceId,
                            deviceType,
                            homeId,
                            EventTime,
                            Reading
                        FROM iotsample_GarageDoor_status_accumulated
                        WHERE hour(EventTime) = hour(current_timestamp());

--DataXQuery--
iotsample_GarageDoor_status_accumulated = SELECT deviceId, deviceType, homeId, EventTime, Reading
FROM GarageDoorAccumalator;

--DataXQuery--
GarageDoorOpenInAnHour = SELECT COUNT(DISTINCT EventTime) AS MinsGarageOpenedInHour,
                                    MAX(EventTime) AS EventTime
                                FROM iotsample_GarageDoor_status_accumulated
                                WHERE homeId = 150
                                    AND deviceType = 'GarageDoorLock'
                                    AND Reading = 0
                                    AND unix_timestamp() - to_unix_timestamp(EventTime,'yyyy-MM-dd') < 3600;

--DataXQuery--
GarageDoorMinutesOpenedFiltered = SELECT
											MAX(eventTimeStamp) AS EventTime,
											COUNT(DISTINCT eventTimeStamp) AS MinutesCount
										FROM DeviceWindowedInput
										WHERE homeId = 150
											AND deviceType = 'GarageDoorLock'
											AND status = 0;

--DataXQuery--
GarageDoorMinutesOpenedIn5minutes = CreateMetric(GarageDoorMinutesOpenedFiltered, MinutesCount);

--DataXQuery--
GarageOpenForFiveMinsWindowAlert = SELECT
                                EventTime AS EventTime,
                                'GarageOpenForFiveMinsWindowAlert' AS MetricName,
                                0 AS Metric,
                                'iotsample' AS Product, 
                                'Garage door opened for >=5 mins' AS Pivot1
                                FROM GarageDoorMinutesOpenedIn5minutes
                                WHERE Metric >= 5;

--DataXQuery--
GarageMinutesOpenedInAnHour = CreateMetric(GarageDoorOpenInAnHour, MinsGarageOpenedInHour);

--DataXQuery--
GarageOpenFor30MinutesInHourThresholdAlert = SELECT 
                                EventTime AS EventTime,
                                'GarageOpenFor30MinutesInHourThresholdAlert' AS MetricName,
                                0 AS Metric,
                                'iotsample' AS Product, 
                                CONCAT('Garage door opened for >= 30 minutes in last hour: ', Metric) AS Pivot1
                                FROM GarageMinutesOpenedInAnHour
                                WHERE Metric >= 30;

--DataXQuery--
Rules = ProcessRules(DataXProcessedInput);

OUTPUT DoorLockStatusOneForLocked TO Metrics;
OUTPUT DoorUnlockedSimpleAlert TO Metrics;
OUTPUT GarageDoorStatusOneForLocked, GarageDoorMinutesOpenedIn5minutes, GarageMinutesOpenedInAnHour TO Metrics;
OUTPUT GarageOpenForFiveMinsWindowAlert TO Metrics;
OUTPUT GarageOpenFor30MinutesInHourThresholdAlert TO Metrics;
OUTPUT HeaterStateOneIsOn, WindowLockStateOneIsLocked, WindowLockStateWindowed TO Metrics;
OUTPUT WindowOpenFiveMinWhileHeaterOnCombinedAlert TO Metrics;