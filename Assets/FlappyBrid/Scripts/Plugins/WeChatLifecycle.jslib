mergeInto(LibraryManager.library, {
    RegisterWeChatLifecycle: function () {
        // 检查是否在微信小游戏环境中
        if (typeof wx === 'undefined') {
            console.warn('WeChatLifecycle: wx对象不存在，可能不在微信小游戏环境中');
            return;
        }

        // 定义发送消息的函数
        function sendMessageToUnity(methodName) {
            try {
                // 尝试多种方式调用Unity的SendMessage
                if (typeof Module !== 'undefined' && Module.SendMessage) {
                    Module.SendMessage('GamePauseManager', methodName, '');
                } else if (typeof window.unityInstance !== 'undefined' && window.unityInstance.SendMessage) {
                    window.unityInstance.SendMessage('GamePauseManager', methodName, '');
                } else if (typeof SendMessage !== 'undefined') {
                    SendMessage('GamePauseManager', methodName, '');
                } else {
                    console.warn('WeChatLifecycle: 无法找到Unity消息发送方法');
                }
            } catch (e) {
                console.error('WeChatLifecycle: 发送消息失败', e);
            }
        }

        // 监听游戏隐藏事件（切屏、弹窗等）
        wx.onHide(function() {
            console.log('WeChatLifecycle: 游戏隐藏，触发暂停');
            sendMessageToUnity('OnGameHide');
        });

        // 监听游戏显示事件（返回游戏）
        wx.onShow(function(res) {
            console.log('WeChatLifecycle: 游戏显示，触发恢复');
            sendMessageToUnity('OnGameShow');
        });

        console.log('WeChatLifecycle: 已注册微信小游戏生命周期事件');
    }
});
