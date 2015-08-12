// mongeez formatted javascript

// changeset phipps:documents2-Core.TenantId_1_DocumentStatus.StatusFlags_1_Core.PublishSetCode_LC_1 runAlways:true
db.documents2.ensureIndex({
    "Core.TenantId": 1,
    "DocumentStatus.StatusFlags": 1,
    "Core.PublishSetCode_LC": 1
},
{
    background : true
});

// changeset phipps:documents2-Core.TenantId_1_Core.PublishSetCode_LC_1 runAlways:true
db.documents2.ensureIndex({

    "Core.TenantId": 1,
    "Core.PublishSetCode_LC": 1
},
{
    background: true
});

// changeset phipps:documents2-Init_PublishSetCode_LC-3
db.documents2.update({
    "Core.PublishSetCode_LC": {
        $exists: false
    }
}, {
    $set: {
        "Core.PublishSetCode_LC": "__empty__"
    }
}, {
    multi: true
});



db.documents2.update({
    "Core.PublishSetCode_LC": {
        $exists: false
    }
}, {
    $set: {
        "Core.PublishSetCode_LC": "__empty__"
    }
}, {
    multi: true
});



// changeset phipps:documents2-ContentSummary.CorrelationId-index runAlways:true
db.documents2.ensureIndex({

    "ContentSummary.CorrelationId": 1
},
{
    background: true
});



