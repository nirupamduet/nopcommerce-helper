using Nop.Core.Domain.Media;
using Nop.Data;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Tasks;
using System;

namespace Nop.Services.Media
{
    /// <summary>
    /// Represents a task to transfer Picture from DB to File
    /// </summary>
    public partial class TransferPictureToAwsTask : ITask
    {
        private readonly MediaSettings _mediaSettings;
        private readonly IPictureService _pictureService;
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;
        private readonly IDbContext _dbContext;

        public TransferPictureToAwsTask(MediaSettings mediaSettings,
            IPictureService pictureService,
            ISettingService settingService,
            ILogger logger,
            IDbContext dbContext)
        {
            this._mediaSettings = mediaSettings;
            this._pictureService = pictureService;
            this._settingService = settingService;
            this._logger = logger;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        public virtual void Execute()
        {
            if (_mediaSettings.EnableTransfer)
            {
                var startId = _mediaSettings.ImgTransferedToFileUptoPictureId + 1;
                var endId = startId + _mediaSettings.BatchTransferImageCount;

                //check if all img already in fileSystem
                if(!_pictureService.StoreInDb)
                {
                    _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, $"Default picture location has been set to File#no operation here", "Default loc of image set to file##no operation here");
                    return;
                }

                //check for max limit exceed
                if (startId > _mediaSettings.MaxImgTransferedPictureId)
                {
                    _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, $"Batch Job FINISHED ## max limit reached", "Batch Job FINISHED");
                    return;
                }

                _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, $"Batch Job Started {startId} to {endId} ## start time : {DateTime.Now.ToString()}", "Batch Job Started");

                for (var i = startId; i <= endId; i++)
                {
                    var picture = _pictureService.GetPictureById(i);
                    if (picture != null)
                    {
                        //transfer to file storage
                        var pictureBinary = _pictureService.LoadPictureBinaryTransferVersion(picture,true);

                        //just update a picture (all required logic is in UpdatePicture method)
                        _pictureService.UpdatePictureTransferVersion(picture.Id,
                                      pictureBinary,
                                      picture.MimeType,
                                      picture.SeoFilename,
                                      true,
                                      false);
                        //we do not validate picture binary here to ensure that no exception ("Parameter is not valid") will be thrown when "moving" pictures
                        _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, $"Picture Transferred to File {i}", "Picture Transferred to File");
                    }
                    else
                    {
                        _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, $"Picture not found ## {i}","Picture Not Found");
                    }
                    _mediaSettings.ImgTransferedToFileUptoPictureId = i;
                    _settingService.SaveSetting(_mediaSettings);
                }

                _logger.InsertLog(Core.Domain.Logging.LogLevel.Information, $"Batch Job Ended {startId} to {endId} ## end time : {DateTime.Now.ToString()}", "Batch Job Ended");
            }
        }
    }
}
