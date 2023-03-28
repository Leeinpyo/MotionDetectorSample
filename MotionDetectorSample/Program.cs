using System;
using System.Drawing;
using System.IO;
using System.Timers;
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Vision.Motion;
using Accord.Video.FFMPEG;

namespace MotionDetectorSample
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 카메라 디바이스 선택
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                var videoDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);

                // 모션 디텍터 생성
                var motionDetector = new MotionDetector(new TwoFramesDifferenceDetector(), null);

                // 비디오 파일 라이터 생성
                var videoWriter = new VideoFileWriter();

                // 녹화 상태 변수
                var isRecording = false;

                // 타이머 생성
                var timer = new Timer(1 * 60 * 1000);
                timer.Elapsed += (sender, eventArgs) =>
                {
                    // 녹화 중지
                    if (isRecording)
                    {
                        isRecording = false;
                        videoWriter.Close();
                    }
                    // 타이머 중지
                    timer.Stop();
                };

                // 카운트다운 변수
                var countdown = (int)timer.Interval / 1000;

                // 카운트다운 타이머 생성
                var countdownTimer = new Timer(1000);
                countdownTimer.Elapsed += (sender, eventArgs) =>
                {
                    countdown--;
                    Console.WriteLine("카운트다운: " + countdown);
                };

                // 비디오 소스에 이벤트 핸들러 추가
                videoDevice.NewFrame += (sender, eventArgs) =>
                {
                    // 모션 감지
                    var motionLevel = motionDetector.ProcessFrame(eventArgs.Frame);
                    if (motionLevel > 0.02)
                    {
                        Console.WriteLine("움직임 감지됨!");

                        // 녹화 시작
                        if (!isRecording)
                        {
                            isRecording = true;

                            // 파일 경로 지정
                            var fileName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".avi";
                            var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), fileName);
                            videoWriter.Open(filePath, eventArgs.Frame.Width, eventArgs.Frame.Height);
                        }

                        // 프레임 저장
                        videoWriter.WriteVideoFrame(eventArgs.Frame);

                        // 타이머 재설정
                        timer.Stop();
                        timer.Start();

                        // 카운트다운 재설정
                        countdown = (int)timer.Interval / 1000;
                        countdownTimer.Stop();
                        countdownTimer.Start();
                    }
                    else
                    {
                        // 녹화 중지
                        if (isRecording && !timer.Enabled)
                        {
                            isRecording = false;
                            videoWriter.Close();

                            // 카운트다운 중지
                            countdownTimer.Stop();
                        }
                    }
                };

                // 비디오 소스 시작
                videoDevice.Start();

                Console.WriteLine("Press any key to stop...");
                Console.ReadKey();

                // 비디오 소스 정지
                videoDevice.Stop();

                // 비디오 파일 라이터 정리
                if (isRecording)
                {
                    videoWriter.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }
    }
}