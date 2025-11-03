# -*- coding: utf-8 -*-

# -*- coding: utf-8 -*-
# -*- coding: utf-8 -*-
import os
import cv2
import glob
import torch
import numpy as np
from torch import nn
from models7 import FastDVDnet7  # make sure FastDVDnet is importable

# ----------------------
# Parameters
# ----------------------
dname = r"D:\data\code\python\Office\fastdvdnet\test_data2"
dout = r"D:\data\code\python\Office\fastdvdnet\result2_ori"
noise_level=20
w_ori=0.0
model_path = "model_ori.pth"



dname = r"D:\data\code\python\Office\fastdvdnet\data_90"
dout = r"D:\data\code\python\Office\fastdvdnet\result90_u7"
noise_level=15
w_ori=0.0
model_path = "logs_u_7/net.pth"



dname = r"D:\data\code\python\Office\fastdvdnet\data129"
dout = r"D:\data\code\python\Office\fastdvdnet\result129_u7"
noise_level=10
w_ori=0.0
model_path = "logs_u_7/net.pth"

device_force_cpu = False



dname = r"D:\data\code\python\Office\fastdvdnet\data134"
dout = r"D:\data\code\python\Office\fastdvdnet\result134_7"
noise_level=10
w_ori=0.0
model_path = "logs_u_7/net.pth"
device_force_cpu = False
add_noise=0




#=================================


os.makedirs(dout, exist_ok=True)


if device_force_cpu:
    print("device = cpu")
    device = torch.device("cpu")
else:
    print("device = auto")
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")


import torch.nn.functional as F

def pad_to_multiple_of_4(tensor):
    _, _, h, w = tensor.shape
    pad_h = (4 - h % 4) if h % 4 != 0 else 0
    pad_w = (4 - w % 4) if w % 4 != 0 else 0
    if pad_h > 0 or pad_w > 0:
        tensor = F.pad(tensor, (0, pad_w, 0, pad_h), mode='reflect')
    return tensor, pad_h, pad_w


# ----------------------
# Load model
# ----------------------
model = FastDVDnet7()
# only wrap in DataParallel if GPU is used
if not device_force_cpu:
    model = nn.DataParallel(model).to(device)
else:
    model = model.to(device)


state_temp_dict = torch.load(model_path, map_location=device)

if device_force_cpu:
    # Remove 'module.' prefix if present
    from collections import OrderedDict
    new_state_dict = OrderedDict()
    for k, v in state_temp_dict.items():
        name = k.replace("module.", "")  # remove the module prefix
        new_state_dict[name] = v
    state_temp_dict = new_state_dict  # ✅ use the cleaned dict


model.load_state_dict(state_temp_dict)
model.eval()

# ----------------------
# Load images
# ----------------------
img_files = sorted(glob.glob(os.path.join(dname, "*.png")))
print(f"Found {len(img_files)} frames")

window = []
window_size = 7

# ----------------------
# Inference
# ----------------------
for idx, f in enumerate(img_files):

    img = cv2.imread(f, cv2.IMREAD_COLOR)
    img = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    img = img.astype(np.float32) / 255.0
    window.append(img)

    # process only when we have 5 frames
    if len(window) == window_size:
        data = np.stack(window, axis=0)  # [5, H, W, 3]
        data = torch.from_numpy(data).permute(0, 3, 1, 2)  # [5, 3, H, W]
        data = data.reshape(1, -1, data.shape[2], data.shape[3]).to(device)  # [1, 15, H, W]

        # zero noise map (for clean inference)
        noise_map = torch.ones((1, 1, data.size(2), data.size(3)), device=device)*noise_level/255

        data, pad_h, pad_w = pad_to_multiple_of_4(data)
        noise_map, _, _ = pad_to_multiple_of_4(noise_map)

        with torch.no_grad():
            out = model(data, noise_map)

        # Crop back
        if pad_h > 0:
            out = out[:, :, :-pad_h, :]
        if pad_w > 0:
            out = out[:, :, :, :-pad_w]

        # Extract the center frame (the reference frame)
        temp_psz = 5  # or your temporal patch size
        ctrl_fr_idx = temp_psz // 2
        ctrl_fr_idx = 4
        C = 3  # RGB

        # Get the original center frame
        data_center = data[:, ctrl_fr_idx*C:(ctrl_fr_idx+1)*C, :out.shape[2], :out.shape[3]]

        # Fuse denoised + original
        out = w_ori * data_center + (1 - w_ori) * out


        out_img = out[0].permute(1, 2, 0).cpu().numpy()
        out_img = np.clip(out_img * 255.0, 0, 255).astype(np.uint8)
        out_img = cv2.cvtColor(out_img, cv2.COLOR_RGB2BGR)
        cv2.imwrite(os.path.join(dout, f"{idx:04d}.png"), out_img)

        window.pop(0)
