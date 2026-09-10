# 按厂商独立适配器，不采用通用 GenICam 抽象

Status: accepted

设备抽象层按厂商（海康、Basler、大恒等）各写一个适配器类，各自封装自家 SDK，对外暴露统一的强类型最小契约；不做"一个通用 GenICam 适配器驱动所有相机"的抽象。原因：通用 GenICam 的字符串键节点模型（`SetFloatValue("ExposureTime", …)`）啰嗦、无类型、用起来不顺手；把厂商差异收敛进适配器内部后，上层 App 只面对强类型方法。

## Considered Options

- **通用 GenICam 适配器**：一个类走 GenICam 节点树驱动所有 GenICam 相机。弃用——节点模型啰嗦难用，参数无类型。
- **按厂商适配器（选定）**：每个 SDK 一个适配器类，厂商私有 API 翻译成强类型契约。

## Consequences

- 新增一个厂商 = 新增一个适配器类 + 一条 `(设备类型, 厂商)` 到适配器的注册映射。
- 某厂商 SDK 升级只影响它自己的适配器，不波及其它厂商和上层 App。
