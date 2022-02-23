package datax.input

import datax.config.SettingDictionary
import datax.data.FileInternal
import datax.input.BlobPointerInput.{extractTimeFromBlobPath, pathHintsFromBlobPath}

object BatchBlobInput {
  def filePathToInternalFileInfo(path: String, dict: SettingDictionary): FileInternal = {
    val inputConf = BlobPointerInputSetting.getInputConfig(dict)
    if (inputConf.fileTimeRegex != null) {
      FileInternal(inputPath = path,
        outputFolders = null,
        outputFileName = pathHintsFromBlobPath(path, inputConf.blobPathRegex.r),  // get Partition in properties
        fileTime = extractTimeFromBlobPath(path, inputConf.fileTimeRegex.r, inputConf.fileTimeFormat),  // get InputTime in properties
        ruleIndexPrefix = "",
        target = null
      )
    } else {
      FileInternal(inputPath = path,
        outputFolders = null,
        outputFileName = null,
        fileTime = null,
        ruleIndexPrefix = "",
        target = null
      )
    }
  }
}
