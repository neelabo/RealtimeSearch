using NeeLaboratory.RealtimeSearch.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace NeeLaboratory.RealtimeSearch.Services
{
    public class PersistAndRestoreService
    {
        private readonly IFileService _fileService;
        private readonly ApplicationInfoService _appInfo;


        public PersistAndRestoreService(IFileService fileService, ApplicationInfoService appInfo)
        {
            _fileService = fileService;
            _appInfo = appInfo;
        }


        private string FolderPath => _appInfo.LocalApplicationDataPath;

        private string AppConfigFileName => _appInfo.ProductName + ".app.json";


        // TODO ASync
        public AppConfig? Load()
        {
            try
            {
                return _fileService.Read<AppConfig>(FolderPath, AppConfigFileName);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new ApplicationException("設定の読み込みに失敗しました。", ex);
            }
        }

        // TODO: ASync
        public void Save(AppConfig appConfig)
        {
            try
            {
                _fileService.Write(FolderPath, AppConfigFileName, appConfig, false);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                throw new ApplicationException("設定の保存に失敗しました。", ex);
            }
        }
    }



}
