﻿using System;
using System.Drawing;
using System.IO;
using System.Timers; // System.Timers.Timer 네임스페이스 추가
using Accord.Video;
using Accord.Video.DirectShow;
using Accord.Vision.Motion;
using Accord.Video.FFMPEG;
using System.Threading;

namespace MotionDetectorSample
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // 카메라 디바이스 선택
                VideoCaptureDevice videoDevice = null;
                var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                var videoDeviceAvailable = new ManualResetEvent(false); // 새로운 ManualResetEvent 생성

                var checkDevicesThread = new Thread(() =>
                {
                    foreach (FilterInfo device in videoDevices)
                    {
                        var tempDevice = new VideoCaptureDevice(device.MonikerString);
                        if (!tempDevice.IsRunning)
                        {
                            videoDevice = tempDevice;
                            videoDeviceAvailable.Set(); // 비디오 디바이스 사용 가능 상태 설정
                            break;
                        }
                    }

                    if (videoDevice == null)
                    {
                        Console.WriteLine("All video devices are in use.");
                    }
                });
                checkDevicesThread.Start();

                // 모션 디텍터 생성
                var motionDetector = new MotionDetector(new TwoFramesDifferenceDetector(), null);

                // 비디오 파일 라이터 생성
                var videoWriter = new VideoFileWriter();

                // 녹화 상태 변수
                var isRecording = false;

                // 타이머 생성
                var timer = new System.Timers.Timer(10 * 1000);
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
                var countdownTimer = new System.Timers.Timer(1000);
                countdownTimer.Elapsed += (sender, eventArgs) =>
                {
                    if (countdown <= 0)
                    {
                        Console.WriteLine("영상 저장됨");
                        countdownTimer.Stop();
                    }
                    else
                    {
                        Console.WriteLine("카운트다운: " + countdown);
                        countdown--;
                    }
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
                        if (countdownTimer.Enabled)
                        {
                            countdownTimer.Stop();
                        }
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
                        else if (isRecording && timer.Enabled && (motionLevel <= 0.02))
                        {
                            // 프레임 저장
                            videoWriter.WriteVideoFrame(eventArgs.Frame);
                        }   
                    }
                };

                // 비디오 소스 시작
                videoDevice.Start();

                Console.WriteLine("Press ESC key to stop...");
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        break;
                    }
                }

                // 비디오 파일 라이터 정리
                if (isRecording)
                {
                    videoWriter.Close();
                }

                // 비디오 소스 정지
                videoDevice.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.ToString());
            }
        }
    }
}