   M S B A M L   `   `   `  ����  
Kinect_AMX6    0Microsoft.Samples.Kinect.SpeechBasics.MainWindow   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   -   5          � Title$  Kinect-Amx Room Control��5   	   $��487.5��6)   $	��630��68    � Icon$) #Images/WSU-Central_Social-Badge.png��5   7    � 
ResizeMode$ CanMinimize=�6b   $
��Bold"�6{    � WindowStyle$ ThreeDBorderWindow=�6�    � WindowStartupLocation$ CenterScreen=�6�   $y�Visible=�6�    � WindowState$ 	Minimized=�6�    � 	Resources 5         MediumGreyBrush �� Color  LightGreyBrush  KinectPurpleBrush �� 
TargetType	 �� Property
 �� Value��%�   &	        &	 #     &	 F     '�� i     �� 5   
   $ 	#ff6e6e6e��62   �� 5   
   $ 	#ffd2d2d2��61   �� 5	   
   $ 	#ff52318f��64   �� 5
   
   " ��6   .�5      �� 	 �@J�SnapsToDevicePixels
 . 64   5      5      ��5      �� 5   
    �� ImageSource$ Images/campus.jpg��6    �� Opacity$	 0.8Q�65    J� Effect 5      3  -System.Windows.Media.Effects.DropShadowEffect  5   
   .��5      � $��	10 0 10 0q�6   ��5   
   �� 5      $ 	#FFF5EBEB��6   $ 0Q�60    � RowDefinitions 5   
   �� 5      $
��Auto �6   $��75��6*   �� 5      �� 5      $
��Auto �6   �� 5      $
��Auto �6   .+�5   
   ]� $��0��6   .+�5       �� $)��#Images/WSU-Spok-Logo_Hrzn-RGB-2.jpg��6   $
��Fill=�6A   $	��529��6P   $��57��6\    J� Opacity$	 0.9Q�6h   $��0,5,0,0q�6v   $	��Top=�6�   $
��Left=�6�    5!        5"      5$      �� 5%      -   5         ��Name$ image��5%      $��57��6#   $��0,5,0,0q�6/   $��79��6@   $��Images/Status.png��6K   $	��Top=�6f    5&        5'      5)      5*      �� 5+   
   -   5         ��Name$ textBox��5+      $
��Left=�6#   $��44��6>   $��
10,322,0,0q�6J   $��1��6^    �� TextWrapping$
 Wrap=�6k   $��Command Output��6   $	��Top=�6�   $	��584��6�   $
��Bold"�6�   $��14(�6�   $��Italic$�6�   ���	#FFEEE5E5���	#FFF10D0D$
 0.65Q�6  �� 5,   
   -   5        $ ipBox��5,      $
��Left=�6!   $��40��6<   $��
10,277,0,0q�6H   $��1��6\   $
 Wrap=�6i   $��IP... ��6}   $	��Top=�6�   $	��255��6�   $��14(�6�   .��6�   ��5-      �� 5.      $ White��6"   $	 0.4Q�60   .��5/   "   50      �� 51   
   -   5         ��Name$ ipLabel��51      $��Enter The Room IP here:��6!   $
��Left=�6C   $��
10,242,0,0q�6^   $��1��6r   $	��Top=�6   $	��255��6�   $
��Bold"�6�   $��17(�6�   $��Rockwell Extra Bold)�6�   $��Italic$�6�   ���	#FF060505��52      �� 53      $ 	#FF9E9E9E��6"   $	 0.8Q�64   55      �� 56   
   -   5         ��Name$ 
saveButton��56      $�� ��6%   $
��Left=�60   $��271,277,0,0q�6K   $��1��6`   $	��Top=�6m   $��50��6�   $��40��6�   ��6�   Z� ��57      �� 58       �� EndPoint$ 0.5,1&�6&    �� 
StartPoint$ 0.5,0&�67   .��59      �  � Color$ Black��6#    � Offset$ 0Q�61   � 5:      $ White��6#   $ 1Q�61   5;       5=        5>      ��5@      �� 5A      $ Images/save.jpg��6   5C      �� 5D   
   -   5        $ resetButton��5D      $�� ��6&   $
��Left=�61   $��326,277,0,0q�6L   $��1��6a   $	��Top=�6n   $��59��6�   $��40��6�    5E        5F      ��5H      �� 5I      $ Images/reset.jpg��6   5K      5L      5M      