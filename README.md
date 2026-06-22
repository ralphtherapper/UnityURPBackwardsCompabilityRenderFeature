This repository contains one script that attempts to migrate old built-in renderpipepline or older URP with compatibility mode set on to new URP 17.3. Reason for this is that Unity finally did truly awful thing and forced URP CameraSTacking which makes things much worse if the project uses multiple stacked cameras (espicially when in multiple scenes).

This seems to work in editor but still doing early tests.

### Techincal specs

This is implemented by SRP RenderFeature which does the following things:
1) Blits rendered camera texture to buffer texture
2) Blits the buffer texture to the camera background when next camera renderers
3) Due camera clear does not function correctly it also implements pass to clear the camera activeColorBuffer with the camera settings (NOTE: I only implemented solid color not skybox)

Script can probably be improved like:
1) Not preventing blitting last frame if current camera does clear the color buffer anyway...
2) API document is for me difficult to read and some of its outdated. Hence there is probably things that are not required.
3) Because aggressive URP development expect this to break in next version.
