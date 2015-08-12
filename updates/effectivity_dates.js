// mongeez formatted javascript


// changeset chet:init-EffectivityRange-rev4
db.documents2.update({
    "Draft": null
}, {
    $set: {
        "Draft": {}
    }
}, {
    multi: true
});

db.documents2.update({
    "Draft.EffectivityRange": {
        $exists: false
    }
}, {
    $set: {
        "Draft.EffectivityRange": {
            "_startDate": ISODate("1970-01-01T00:00:00Z"),
            "_endDate": ISODate("3000-01-01T00:00:00Z")
         }
    }
}, {
    multi: true
});

db.documents2.update({
    "Active": null
}, {
    $set: {
        "Active": {}
    }
}, {
    multi: true
});

db.documents2.update({
    "Active.EffectivityRange": {
        $exists: false
    }
}, {
    $set: {
        "Active.EffectivityRange": {
            "_startDate": ISODate("1970-01-01T00:00:00Z"),
            "_endDate": ISODate("3000-01-01T00:00:00Z")
        }
    }
}, {
    multi: true
});


// changeset chet:rename_effectivitydate_to_activedateRange-3
db.documents2.update(
    { "Active": { $exists: true, $not: { $type: 10 } } },
    {
        $rename: {
            "Active.EffectivityRange": "Active.ActiveDateRange",
        }
    },
    {multi:true}
);

db.documents2.update(
    { "Draft": { $exists: true, $not: { $type: 10 } } },
    {
        $rename: {
            "Draft.EffectivityRange": "Draft.ActiveDateRange",
        }
    },
    {multi:true}
);

db.documents2.update(
    { "ActiveForDiscard": {$exists : true, $not : {$type : 10}} },
    {
        $rename: {
            "ActiveForDiscard.EffectivityRange": "ActiveForDiscard.ActiveDateRange",
        }
    },
    {multi:true}
);

db.documents2.update(
    { "DraftToPublish": { $exists: true, $not: { $type: 10 } } },
    {
        $rename: {
            "DraftToPublish.EffectivityRange": "DraftToPublish.ActiveDateRange",
        }
    },
    {multi:true}
);

// changeset chet:drop_old_effdate_indexes
db.documents2.dropIndex({
    "Core.TenantId": 1,
    "Core.DocumentListId": 1,
    "Active.EffectivityRange._startDate": 1,
    "Active.EffectivityRange._endDate": 1,
});

db.documents2.dropIndex({
    "Core.TenantId": 1,
    "Core.DocumentListId": 1,
    "Draft.EffectivityRange._startDate": 1,
    "Draft.EffectivityRange._endDate": 1,
});

// changeset chet:activedateranges runAlways:true
db.documents2.ensureIndex({
    "Core.TenantId": 1,
    "Core.DocumentListId": 1,
    "Active.ActiveDateRange._startDate": 1,
    "Active.ActiveDateRange._endDate": 1,
},
{
    background: true
});

// changeset chet:draftdateranges runAlways:true
db.documents2.ensureIndex({
    "Core.TenantId": 1,
    "Core.DocumentListId": 1,
    "Draft.ActiveDateRange._startDate": 1,
    "Draft.ActiveDateRange._endDate": 1,
},
{
    background: true
});